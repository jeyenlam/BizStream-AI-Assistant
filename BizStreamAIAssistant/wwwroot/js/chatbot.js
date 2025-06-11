document.addEventListener("DOMContentLoaded", init);

function init() {
    const form = document.getElementById("chatForm");
    const queryInput = document.getElementById("queryInput");
    const messageContainer = document.getElementById("messageContainer");
    const chatbotButton = document.getElementById("chatbotButton");
    const retryButtons = document.getElementsByClassName("retryButton");
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

    // Array.from(retryButtons).forEach(button => {
    //     button.addEventListener("click", (e) => {
    //         const retryMessageId = Array.from(button.classList).find(cls => cls.startsWith("botMessage"));
    //         if (!retryMessageId) throw new Error("Retry button does not have a valid message ID class");

    //         const retryUserQuery = document.querySelector(`.userMessage${retryMessageId} .userMessageText`).textContent;
    //         handleMessageSubmit(retryUserQuery);
    //     });
    // });
    document.addEventListener("click", (e) => {
        const button = e.target.closest(".retryButton");
        if (!button) return;

        const retryMessageId = Array.from(button.classList).find(cls => cls.startsWith("botMessage"))?.replace("botMessage", "");
        if (!retryMessageId) return;

        const retryUserQuery = document.querySelector(`.userMessage${retryMessageId} .userMessageText`)?.textContent;
        if (!retryUserQuery) return;

        handleMessageSubmit(retryUserQuery);
    });


    chatbotButton.addEventListener("click", toggleChatbot);
    form.addEventListener("submit", (e) => {
        e.preventDefault();
        handleMessageSubmit();
    });

    queryInput.addEventListener("keydown", (e) => {
        if (e.key === "Enter" && queryInput.value.trim()) {
            e.preventDefault();
            handleMessageSubmit();
        }
    });

    function toggleChatbot() {
        form.classList.toggle(CLASSES.hidden);
        form.classList.toggle(CLASSES.slideUp);
        chatbotButton.classList.toggle(CLASSES.spinOnce);
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

    function addBotMessage(message, optionsDisabled = false) {
        addMessageToUI({
            text: message,
            templateId: TEMPLATE_IDS.bot,
            cloneClass: CLASSES.botClone,
            textSelector: ".botMessageText",
            messageId: optionsDisabled ? ``: `botMessage${botMessageId++}`,
            optionsDisabled
        });

        if (optionsDisabled) return;
        messages.push({ role: "assistant", content: message });
    }

    function addMessageToUI({ text, templateId, cloneClass, textSelector, messageId, optionsDisabled }) {
        const template = document.getElementById(templateId);
        if (!template) return console.error(`Missing template: ${templateId}`);

        const clone = template.cloneNode(true);
        clone.classList.remove(CLASSES.hidden);
        clone.removeAttribute("id");
        clone.classList.add(cloneClass);

        const hasLink = extractLinks(text).length > 0;
        const finalText = hasLink ? linkifyText(text) : text;

        clone.querySelector(textSelector).innerHTML = finalText;
        messageContainer.appendChild(clone);

        if (optionsDisabled) {
            const options = clone.querySelector(".botMessageOptions");
            options.classList.add(CLASSES.hidden);
            return;
        }

        // Add messageID to elements of each message container for later reference (used for retry/copy functions)
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

    function extractLinks(text) {
        const urlRegex = /(https?:\/\/[^\s]+)/g;
        return text.match(urlRegex) || [];
    }

    function linkifyText(text) {
        const urlRegex = /(https?:\/\/[^\s]+)/g;
        return text.replace(urlRegex, (url) =>
            `<a href="${url}" target="_blank" rel="noopener noreferrer" class="font-semibold">${url}</a>`
        );
    }

    function welcomeMessage() {
        const welcomeText = "Hi there👋 I'm BZSAI, an AI assistant of the BizStream website 😁. You can ask me anything about BizStream!";
        addBotMessage(welcomeText, true);
    }
}
