document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("chatForm");
    const queryInput = document.getElementById("queryInput");
    const messageContainer = document.getElementById("messageContainer");

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
        queryInput.value = "";

        // Simulate bot response
        setTimeout(() => {
            const botResponse = "This is a simulated response to: " + userMessage;
            addMessageToUI({ role: "bot", text: botResponse });
        }, 1000);

        fetch("/api/chatbot", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ message: userMessage })
        }).then(response => {   
            if (!response.ok) {
                throw new Error("Network response was not ok");
            }
            return response.json();
        }).then(data => {       
            const botResponse = data.response || "No response from server";
            addMessageToUI({ role: "bot", text: botResponse });
        }).catch(error => {
            console.error("Error:", error);
            addMessageToUI({ role: "bot", text: "Error: " + error.message });
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
