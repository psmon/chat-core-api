
document.addEventListener("DOMContentLoaded", () => {

    console.log("DOMContentLoaded");

    // <snippet_Connection>
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub")
        .configureLogging(signalR.LogLevel.Information)
        .build();
    // </snippet_Connection>

    // <snippet_ReceiveMessage>    
    connection.on("ReceiveMessage", (actionType, channelId, channelName, eventData) => {
        const li = document.createElement("li");
        li.textContent = `OnRecvMessage ${actionType} : ${channelId} ${channelName} ${eventData}`;
        document.getElementById("messageList").appendChild(li);
    });
    // </snippet_ReceiveMessage>

    // <snippet_ErrorMessage>
    connection.on("ErrorMessage", (errorCode, errMessage) => {
        const li = document.createElement("li");
        li.textContent = `OnErrorMessage ${errorCode}: ${errMessage}`;
        document.getElementById("messageList").appendChild(li);
    });
    // </snippet_ErrorMessage>

    

    document.getElementById("send").addEventListener("click", async () => {
        const actionInput = document.getElementById("actionInput").value;
        const input1 = document.getElementById("input1").value;
        const input2 = document.getElementById("input2").value;
        const input3 = document.getElementById("input3").value;

        // <snippet_Invoke>
        try {
            await connection.invoke(actionInput, input1, input2, input3);
        } catch (err) {
            console.error(err);
        }
        // </snippet_Invoke>
    });

    async function start() {
        try {
            await connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.log(err);
            setTimeout(start, 5000);
        }
    };

    connection.onclose(async () => {
        await start();
    });

    // Start the connection.
    start();
});