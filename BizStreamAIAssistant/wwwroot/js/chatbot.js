document.addEventListener("DOMContentLoaded", init);

function init() {
    const form = document.getElementById("chatForm");
    const queryInput = document.getElementById("queryInput");
    const messageContainer = document.getElementById("messageContainer");
    const chatbotButton = document.getElementById("chatbotButton");
    const messages = [];

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
        spinOnce: "spin-once",
        slideUp: "slide-up-animation"
    };

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

    async function handleMessageSubmit() {
        const userMessage = queryInput.value.trim();
        if (!userMessage) return;

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
            console.error("Error:", error);
            removeTextLoading();
            addBotMessage("Sorry, something went wrong.");
        }
    }

    function addUserMessage(message) {
        addMessageToUI({
            role: "user",
            text: message,
            templateId: TEMPLATE_IDS.user,
            cloneClass: CLASSES.userClone,
            textSelector: ".userMessageText"
        });
        messages.push({ role: "user", content: message });
    }

    function addBotMessage(message) {
        addMessageToUI({
            role: "bot",
            text: message,
            templateId: TEMPLATE_IDS.bot,
            cloneClass: CLASSES.botClone,
            textSelector: ".botMessageText"
        });
        messages.push({ role: "assistant", content: message });
    }

    function addMessageToUI({ text, templateId, cloneClass, textSelector }) {
        const template = document.getElementById(templateId);
        if (!template) return console.error(`Missing template: ${templateId}`);

        const clone = template.cloneNode(true);
        clone.classList.remove(CLASSES.hidden);
        clone.removeAttribute("id");
        clone.classList.add(cloneClass);
        clone.querySelector(textSelector).textContent = text;

        messageContainer.appendChild(clone);
        scrollToBottom();
    }

    function renderTextLoading() {
        const template = document.getElementById(TEMPLATE_IDS.loading);
        if (!template) return;
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
}
