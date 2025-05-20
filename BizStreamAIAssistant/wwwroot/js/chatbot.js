document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("chatForm");
    const queryInput = document.getElementById("queryInput");
    const messageContainer = document.getElementById("messageContainer");
    const messages = [];
    const chatbotButton = document.getElementById("chatbotButton");
    const spinner = document.querySelector('.spinner');

    chatbotButton.addEventListener("click", function () {
        if (form.classList.contains("hidden"))
        {
            form.classList.remove("hidden");
            form.classList.add("slide-up-animation");
        }
        else
        {
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
        console.log("Message history:", messages);
        console.log(JSON.stringify({ messages }, null, 2));

        queryInput.value = "";

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

            
            
            const botResponse = data.text || "No response from server";
            addMessageToUI({ role: "bot", text: botResponse });
            messages.push({ role: "assistant", content: botResponse });
            console.log("Message history:", messages);
        }).catch(error => {
            console.error("Error:", error);
        }); 
    }

    function addMessageToUI({ role, text }) {

        const messageElement = document.createElement("div");

        role == "user" ? messageElement.className = "justify-end rounded-xl p-1 px-2 bg-[#003D78] text-white" : messageElement.className = "justify-start p-1 px-2 bg-[#00C4E9] rounded-xl";
        if (role == "bot") {

            const botProfile = document.createElement("div");
            botProfile.className = "flex items-center gap-2 justify-start";
        
            const botImage = document.createElement("img");            
            botImage.src = "/images/robot (1).png";
            botImage.className = "w-8 h-8 rounded-full bg-[#00C4E9] border-2 p-1 border";

            const botName = document.createElement("span");
            botName.textContent = "BZAI";  

            const messageBubble = document.createElement("div");
            messageBubble.textContent = text;

            botProfile.appendChild(botImage);
            botProfile.appendChild(botName);
            messageElement.appendChild(botProfile);
            messageElement.appendChild(messageBubble);
        } else {
            messageElement.textContent = text;
        }

        messageContainer.appendChild(messageElement);
        messageContainer.scrollTop = messageContainer.scrollHeight;
    }
});
