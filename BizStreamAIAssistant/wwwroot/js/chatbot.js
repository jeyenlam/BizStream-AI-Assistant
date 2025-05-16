document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("chatForm");
    const queryInput = document.getElementById("queryInput");
    const messageContainer = document.getElementById("messageContainer");
    const messages = [];

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
            // body: JSON.stringify({ message: userMessage })
            body: JSON.stringify({ messages })

        }).then(async response => {   
            if (!response.ok) {
                const errorText = await response.text(); // <— reveal real exception message

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
        messageElement.className = role === "user" ? "flex justify-end bg-slate-100" : "flex justify-start";
        messageElement.textContent = text;
        messageContainer.appendChild(messageElement);
        messageContainer.scrollTop = messageContainer.scrollHeight;
    }
});
