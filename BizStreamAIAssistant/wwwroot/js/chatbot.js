document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("chatForm");
    const queryInput = document.getElementById("queryInput");
    const messageContainer = document.getElementById("messageContainer");
    const messages = [];
    const chatbotButton = document.getElementById("chatbotButton");

    chatbotButton.addEventListener("click", function () {
        if (form.classList.contains("hidden")){
            form.classList.remove("hidden");
            form.classList.add("slide-up-animation");
        }
        else {
            form.classList.add("hidden");
            form.classList.remove("slide-up-animation");
        }
        chatbotButton.classList.contains("spin-once") ? chatbotButton.classList.remove("spin-once") : chatbotButton.classList.add("spin-once");
    });

    form.addEventListener("submit", function (event) {
        event.preventDefault(); 
        handleMessageSubmit();
    });

    queryInput.addEventListener("keydown", function (event) {
        if (event.key === "Enter" && queryInput.value.trim() !== "") {
            event.preventDefault();
            handleMessageSubmit();
        }
    });

    function handleMessageSubmit() {
        const userMessage = queryInput.value.trim();
        if (!userMessage) return;

        addMessageToUI({ role: "user", text: userMessage });
        messages.push({ role: "user", content: userMessage });
        queryInput.value = ""; // Clear input field

        setTimeout(() => {
            renderTextLoading();
        }, 1000);

        fetch("/api/chatbot", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ messages })

        }).then(async response => {   
            if (!response.ok) {
                const errorText = await response.text();
                console.error("Error response:", errorText);
                throw new Error("Network response was not ok");
            }
            return response.json();
        }).then(data => {  
            removeTextLoading();
            const botResponse = data.text || "No response from server";
            addMessageToUI({ role: "bot", text: botResponse });
            messages.push({ role: "assistant", content: botResponse });
            console.log("Message history:", messages);
        }).catch(error => {
            console.error("Error:", error);
        }); 
    }

    function addMessageToUI({ role, text }) {
        let clone;

        if (role == "user") {
            clone = document.getElementById("userMessageTemplate").cloneNode(true);
            clone.classList.remove("hidden");
            clone.removeAttribute("id");
            clone.classList.add("clonedUserMessage");
            clone.querySelector(".userMessageText").textContent = text;
        } else {
            clone = document.getElementById("botMessageTemplate").cloneNode(true);
            clone.classList.remove("hidden");
            clone.removeAttribute("id");
            clone.classList.add("clonedBotMessage");
            clone.querySelector(".botMessageText").textContent = text;
        }

        if (clone) messageContainer.appendChild(clone); else console.error("Clone not found");
        messageContainer.scrollTop = messageContainer.scrollHeight;
    }

    function renderTextLoading(){
        const clone = document.getElementById("textLoadingTemplate").cloneNode(true);
        clone.classList.remove("hidden");
        clone.removeAttribute("id");
        clone.classList.add("clonedTextLoading")
        messageContainer.appendChild(clone);
    }

    function removeTextLoading(){
        const loadingText = document.querySelector(".clonedTextLoading");
        if (loadingText) loadingText.remove();
    }
});
