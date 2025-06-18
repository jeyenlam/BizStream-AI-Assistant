document.addEventListener("DOMContentLoaded", init);

function init() {
    const form = document.getElementById("chatForm");
    const queryInput = document.getElementById("queryInput");
    const messageContainer = document.getElementById("messageContainer");
    const chatbotButton = document.getElementById("chatbotButton");
    const hoverPrompt = document.getElementById("hoverPrompt");
    const messages = [];
    var botMessageId = 0;
    var userMessageId = 0;

    const TEMPLATE_IDS = {
        user: "userMessageTemplate",
        bot: "botMessageTemplate",
        loading: "textLoadingTemplate"
    };

    const CLASSES = {
        hidden: "hidden",
        userClone: "clonedUserMessage",
        botClone: "clonedBotMessage",
        loadingClone: "clonedTextLoading",
        spinOnce: "spin-once-animation",
        slideUp: "slide-up-animation"
    };
    
    welcomeMessage();

    document.addEventListener("click", (e) => {
        const button = e.target.closest(".retryButton");
        if (!button) return;

        const messageId = Array.from(button.classList).find(cls => cls.startsWith("botMessage"))?.replace("botMessage", "");
        if (!messageId) return;

        const retryUserQuery = document.querySelector(`.userMessage${messageId} .userMessageText`)?.textContent;
        if (!retryUserQuery) return;

        handleMessageSubmit(retryUserQuery);
    });

    document.addEventListener("click", (e) => {
        const copyButton = e.target.closest(".copyButton");
        
        if (!copyButton) return;

        const botMessageId = Array.from(copyButton.classList).find(cls => cls.startsWith("botMessage"));
        if (!botMessageId) return;

        const messageText = document.querySelector(`.${botMessageId} .botMessageText`).textContent;

        navigator.clipboard.writeText(messageText)
            .then(() => {
                console.log("Message copied to clipboard");

                const copyElement = copyButton.querySelector(".copy");
                if (!copyElement) return;

                copyElement.textContent = "Copied";

                setTimeout(() => {
                    copyElement.textContent = "Copy";
                }, 1000);
            })
            .catch(err => {
                console.error("Failed to copy message: ", err);
            });
    });


    chatbotButton.addEventListener("click", toggleChatbot);
    form.addEventListener("submit", (e) => {
        e.preventDefault();
        handleMessageSubmit();
    });

    queryInput.addEventListener("keydown", (e) => {
        if (e.key === "Enter" && queryInput.value.trim() && !e.shiftKey) {
            e.preventDefault();
            handleMessageSubmit();
        }
    });

    function toggleChatbot() {
        form.classList.toggle(CLASSES.hidden);
        form.classList.toggle(CLASSES.slideUp);
        chatbotButton.classList.toggle(CLASSES.spinOnce);
        hoverPrompt.classList.toggle('group-hover:block');
        setTimeout(() => {
            chatbotButton.classList.toggle(CLASSES.spinOnce);
        },1000);
    }

    async function handleMessageSubmit(userMessage) {
        if (!userMessage){
            userMessage = queryInput.value.trim();
            if (!userMessage) return;
        }

        addUserMessage(userMessage);
        queryInput.value = "";

        setTimeout(() => {
            renderTextLoading();
        }, 1000);

        try {
            const response = await fetch("/api/chatbot", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ messages })
            });
    
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText);
            }
    
            const data = await response.json();

            // console.log("Response from server:", data);
            removeTextLoading();
            addBotMessage(data.text || "No response from server");
        } catch (error) {
            console.error("Error calling server:", error);
            removeTextLoading();
            addBotMessage("Sorry, something went wrong.");
        }
    }

    function addUserMessage(message) {
        addMessageToUI({
            text: message,
            templateId: TEMPLATE_IDS.user,
            cloneClass: CLASSES.userClone,
            textSelector: ".userMessageText",
            messageId: `userMessage${userMessageId++}`
        });
        messages.push({ role: "user", content: message });
    }

    function sanitizeJson(str) {
        return str
            .replace(/[“”]/g, '"')        // curly double → straight
            .replace(/[‘’]/g, "'")        // curly single → straight
            .replace(/{{/g, '{')          // double braces → single
            .replace(/}}/g, '}')          
            .replace(/\s\s+/g, ' ')       // collapse extra spaces
            .trim();                      // trim edges
    }

    function addBotMessage(message, optionsDisabled = false) {
        const referencesTextMatch =
            /References:\s*\n(?<json>\[\s*[\s\S]+?\])/;

        const match = message.match(referencesTextMatch);

        // Strip the reference block out of the visible message
        const fullMessage = message;
        message = message.replace(referencesTextMatch, "").trim();

        let parsedReferences = [];
        if (match?.groups?.json) {
            try {
                const rawJson = sanitizeJson(match.groups.json);
                parsedReferences = JSON.parse(rawJson);
            } catch (err) {
                console.error("Failed to parse references JSON:", err, match.groups.json);
            }
        }

        addMessageToUI({
            text: message,
            references: parsedReferences,
            templateId: TEMPLATE_IDS.bot,
            cloneClass: CLASSES.botClone,
            textSelector: ".botMessageText",
            messageId: optionsDisabled ? "" : `botMessage${botMessageId++}`,
            optionsDisabled
        });

        if (optionsDisabled) return;
        messages.push({
            role: "assistant",
            content: fullMessage,
            references: parsedReferences
        });

        console.log(parsedReferences);
        console.log(messages);
    }

    function addMessageToUI({ text, references, templateId, cloneClass, textSelector, messageId, optionsDisabled }) {
        const template = document.getElementById(templateId);
        if (!template) return console.error(`Missing template: ${templateId}`);

        const clone = template.cloneNode(true);
        clone.classList.remove(CLASSES.hidden);
        clone.removeAttribute("id");
        clone.classList.add(cloneClass);

        const finalText = formatText(text, references);
        clone.querySelector(textSelector).innerHTML = finalText;
        messageContainer.appendChild(clone);

        if (optionsDisabled) {
            const options = clone.querySelector(".botMessageOptions");
            options?.classList.add(CLASSES.hidden);
            return;
        }

        // Add messageID to all elements for tracking
        clone.classList.add(messageId);
        clone.querySelectorAll("*").forEach(node => {
            if (node.classList) {
                node.classList.add(messageId);
            }
        });

        scrollToBottom();
    }


    function renderTextLoading() {
        const template = document.getElementById(TEMPLATE_IDS.loading);
        if (!template) return console.error(`Missingtemplate: ${TEMPLATE_IDS.loading}`);
        const clone = template.cloneNode(true);
        clone.classList.remove(CLASSES.hidden);
        clone.removeAttribute("id");
        clone.classList.add(CLASSES.loadingClone);
        messageContainer.appendChild(clone);
        scrollToBottom();
    }

    function removeTextLoading() {
        const loading = document.querySelector(`.${CLASSES.loadingClone}`);
        if (loading) loading.remove();
    }

    function scrollToBottom() {
        requestAnimationFrame(() => {
            messageContainer.scrollTop = messageContainer.scrollHeight;
        });
    }

    function linkifyText(text) {
        const urlRegex = /(https?:\/\/[^\s]+)/g;
        return text.replace(urlRegex, (url) =>
            `<a href="${url}" target="_blank" rel="noopener noreferrer" class="font-semibold">${url}</a>`
        );
    }

    function formatText(text, references) {
        if (!Array.isArray(references)) {
            references = [];
        }

        let formattedText = text;

        formattedText = linkifyText(formattedText);
        // console.log(`After linkify: ${formattedText}`);

        // Replace page titles and raw URLs from references first
        references.forEach(ref => {
            const { pageTitle, url } = ref;
            console.log(pageTitle);

            if (pageTitle && formattedText.includes(pageTitle)) {
                // console.log(`Linking page title: ${pageTitle} to URL: ${url}`);
                const linkedTitle = `<a href="${url}" target="_blank" rel="noopener noreferrer" class="font-semibold">${pageTitle}</a>`;
                formattedText = formattedText.replaceAll(pageTitle, linkedTitle);
            }
            // console.log(`After linking page title: ${formattedText}`);
        });

        // Replace newlines with <br/>
        formattedText = formattedText.replace(/\n/g, "<br/>");

        // Linkify any *other* plain URLs that weren’t part of references


        return formattedText;
    }

    function welcomeMessage() {
        const welcomeText = "Hi there👋 I'm BZSAI, an AI assistant of the BizStream website 😁. You can ask me anything about BizStream!";
        addBotMessage(welcomeText, true);
    }
}
