"use strict";

/////////////
// Startup //
/////////////

let roomName = "";
let playerName = "";
let gameState = {};

// Canvas Content
const canvas = document.getElementById("GameCanvas");
const ctx = canvas.getContext("2d");

// Game to Canvas Content
const DrawSize = 30; // (30px, 30px) is size of one block on game canvas
const EmptyCode = 0;
const BreakableWallCode = 1;
const UnbreakableWallCode = 2;
const BombCode = 3;

// Set up the SignalR connections to the lobby and chat hubs.
const gameConnection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();
const lobbyConnection = new signalR.HubConnectionBuilder().withUrl("/lobbyHub").build();
const chatConnection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

// On page load, attempt to pre-fill the player name input from browser cookies.
document.addEventListener("DOMContentLoaded", function (event) {
    roomName = getRoomNameFromPath(location.pathname);
    playerName = areCookiesEnabled() ? readCookie("playerName") : getQueryValue("playerName");
});

///////////////////////////////////////////////////////////////
// Event Handlers for the SignalR hub connection of the game //
///////////////////////////////////////////////////////////////

gameConnection.start().then(function () {
    gameConnection.invoke("JoinGameRoom", roomName, playerName).then(function () {
        gameConnection.invoke("RefreshGameState", roomName).catch(function (err) {
            return handleError(err);
        });
    }).catch(function (err) {
        return handleError(err);
    });
});

gameConnection.on("ReceiveGameState", function (state) {
    drawGame(state);
});

////////////////////////////////////////////////////////////////
// Event Handlers for the SignalR hub connection of the lobby //
////////////////////////////////////////////////////////////////

lobbyConnection.start().then(function () {
    lobbyConnection.invoke("RegisterConnection", { roomName, playerName }).catch(function (err) {
        return handleError(err);
    });

    lobbyConnection.invoke("JoinLobbyRoom", roomName).catch(function (err) {
        return handleError(err);
    });
});

lobbyConnection.on("PlayerDisconnected", function (disconnectedPlayerName) {
    console.log("Received disconnection message from " + disconnectedPlayerName);
    addSystemChatMessage(disconnectedPlayerName + " has disconnected.");
});

///////////////////////////////////////////////////////////////
// Event Handlers for the SignalR hub connection of the chat //
///////////////////////////////////////////////////////////////

chatConnection.start().then(function () {
    chatConnection.invoke("JoinChatRoom", roomName).then(function () {
        chatConnection.invoke("GetPlayersInRoom", roomName).then(function (playerNames) {
            let message = "Connected Players: ";
            for (let i = 0; i < playerNames.length; i++) {
                if (i != 0) {
                    message += ", ";
                }
                message += playerNames[i];
                if (playerNames[i] === playerName) {
                    message += " (You)";
                }
            }
            addSystemChatMessage(message);
        }).catch(function (err) {
            return handleError(err);
        });

        chatConnection.invoke("SendPlayerConnected", roomName, playerName).catch(function (err) {
            return handleError(err);
        });
    }).catch(function (err) {
        return handleError(err);
    });
}).catch(function (err) {
    return handleError(err);
});

chatConnection.on("PlayerConnected", function (connectedPlayerName) {
    addSystemChatMessage(connectedPlayerName + " has connected.");
});

chatConnection.on("ReceiveMessage", function (message, sender) {
    addChatMessage(message, sender);
});

//////////////////////////////////////////
// Event Handlers for document elements //
//////////////////////////////////////////

document.getElementById("SendMessageButton").addEventListener("click", function (event) {
    sendMessage();
});

document.getElementById("ClearMessagesButton").addEventListener("click", function (event) {
    document.getElementById("messagesList").innerHTML = "";
});

document.getElementById('ChatMessageInput').addEventListener('keydown', function (event) {
    const maxLength = 30;

    if (event.code === 'Enter') {
        sendMessage();
    }
    else if (isAlphaNumeric(event.key)) {
        if (this.value.length >= maxLength) {
            event.preventDefault();
        }
    }

    event.stopPropagation();
});

// TODO: Adjust this to be focused on only the game canvas
document.addEventListener('keydown', function (event) {
    if (event.defaultPrevented) {
        return;
    }

    if (isGameKeyCode(event.code)) {
        gameConnection.invoke("SendPlayerMove", roomName, playerName, event.keyCode).catch(function (err) {
            return handleError(err);
        });
        event.stopPropagation();
    }
});

/////////////
// Utility //
/////////////

function drawGame(state) {
    // Draw board
    const board = state.board;

    for (let row = 0; row < board.length; row++) {
        for (let col = 0; col < board[row].length; col++) {
            const code = board[row][col];

            if (code == EmptyCode) {
                ctx.fillStyle = "#EDD3C4";
            }
            else if (code == UnbreakableWallCode) {
                ctx.fillStyle = "#080708";
            }
            else if (code == BreakableWallCode) {
                ctx.fillStyle = "#C8ADC0";
            }

            ctx.fillRect(col * DrawSize, row * DrawSize, DrawSize, DrawSize);
        }
    }

    // Draw players
    const otherPlayers = getOtherPlayers(state.players);
    const player = getPlayer(state.players)

    for (let i = 0; i < otherPlayers.length; i++) {
        const otherPlayer = otherPlayers[i];
        ctx.fillStyle = "#F03A47";
        ctx.fillRect(otherPlayer.col * DrawSize, otherPlayer.row * DrawSize, DrawSize, DrawSize);
    }

    ctx.fillStyle = "#37FF8B";
    ctx.fillRect(player.col * DrawSize, player.row * DrawSize, DrawSize, DrawSize);

    // Draw bombs
    for (let i = 0; i < state.bombs.length; i++) {
        const bomb = state.bombs[i];
        const radius = DrawSize / 2;
        ctx.fillStyle = "blue";
        ctx.beginPath();
        ctx.arc(bomb.col * DrawSize + radius, bomb.row * DrawSize + radius, radius, 0, 2 * Math.PI);
        ctx.fill();
    }
}

function addSystemChatMessage(message) {
    var listItem = document.createElement("li");
    listItem.classList.add("list-group-item", "text-break", "mt-2");
    message = message.replace(/</g, "&lt;").replace(/>/g, "&gt;");  // Avoiding script injection.
    listItem.innerHTML = `${message}`;

    document.getElementById("messagesList").appendChild(listItem);
    snapScrollToBottom();
}

function addChatMessage(message, sender) {
    const lastListItem = document.getElementById("messagesList").lastElementChild;
    message = message.replace(/</g, "&lt;").replace(/>/g, "&gt;");  // Avoiding script injection.

    if (lastListItem == null || !lastListItem.classList.contains(sender + "-chatMessage")) {
        const selfSenderMessagePart = (sender === playerName) ? " (You)" : "";
        const listItem = document.createElement("li");
        listItem.classList.add(sender + "-chatMessage", "list-group-item", "text-break", "mt-2");
        listItem.innerHTML = `<b>${sender}${selfSenderMessagePart}:</b></br>` + message;

        document.getElementById("messagesList").appendChild(listItem);
    }
    else {
        lastListItem.innerHTML += `</br>` + message;
    }

    snapScrollToBottom();
}

function sendMessage() {
    const message = document.getElementById("ChatMessageInput").value.trim();

    document.getElementById("ChatMessageInput").value = "";

    if (message.length === 0) {
        return;
    }

    chatConnection.invoke("SendMessage", roomName, message, playerName).catch(function (err) {
        return handleError(err);
    });
}

// Hacky: This will always resolve to the bottom of the scrollbox (minimal scroll top to get to bottom).
function snapScrollToBottom() {
    const scrollbox = document.getElementById("messagesScrollbox");
    scrollbox.scrollTop = scrollbox.scrollHeight;
}

function getOtherPlayers(players) {
    let arr = [];

    for (const key in players) {
        if (players.hasOwnProperty(key)) {
            if (key !== playerName) {
                arr.push(players[key]);
            }
        }
    }

    return arr;
}

function getPlayer(players) {
    for (const key in players) {
        if (players.hasOwnProperty(key)) {
            if (key === playerName) {
                return players[key];
            }
        }
    }

    return null;
}