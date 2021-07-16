"use strict";

/////////////
// Startup //
/////////////

// TODO: Prevent clients from hacking into another lobby by sending names from controller to hub to client.
let roomName = "";   
let playerName = "";
let gameState = {};
let isConnectedToGame = false;

// Canvas Content
const canvas = document.getElementById("GameCanvas");
const ctx = canvas.getContext("2d");

// Game to Canvas Content
const DrawSize = 30; // (30px, 30px) is size of one block on game canvas
const Radius = DrawSize / 2;
const EmptyCode = 0;
const BreakableWallCode = 1;
const UnbreakableWallCode = 2;
const BombCode = 3;

// Set up the SignalR connections to the lobby and chat hubs.
const gameConnection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();
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
        isConnectedToGame = true;
    }).catch(function (err) {
        return handleError(err);
    });
});

gameConnection.on("ReceiveGameState", function (state) {
    drawGame(state);
});

gameConnection.on("ReceiveWinner", function (winnerName) {
    addSystemChatMessage(winnerName + " has won! Do something better with your day now.");
    isConnectedToGame = false;
    gameConnection.stop();
});

gameConnection.on("ReceiveTie", function () {
    addSystemChatMessage("It's a tie. Pathetic.");
    isConnectedToGame = false;
    gameConnection.stop();
});

gameConnection.on("ReceiveEmbarrassment", function () {
    addSystemChatMessage("My guy. You're literally by yourself. How could this have happened?");
    isConnectedToGame = false;
    gameConnection.stop();
});

gameConnection.on("PlaySoundForPlacingBomb", function () {
    stopSound("bombplacement");
    playSound("bombplacement");
});

///////////////////////////////////////////////////////////////
// Event Handlers for the SignalR hub connection of the chat //
///////////////////////////////////////////////////////////////

chatConnection.start().then(function () {
    chatConnection.invoke("JoinChatRoom", roomName, playerName).then(function () {
        chatConnection.invoke("GetPlayersInRoom", roomName).then(function (playerNames) {
            // Handle the case where the connection context has not yet been added for this player
            if (!playerNames.includes(playerName)) {
                playerNames.push(playerName);
            }

            addSystemChatMessage(createPlayerNamesMessage(playerNames));
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

chatConnection.on("PlayerDisconnected", function (disconnectedPlayerName) {
    addSystemChatMessage(disconnectedPlayerName + " has disconnected.");
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

    if (isGameKeyCode(event.code) && isConnectedToGame) {
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

            drawBlock(col, row);
        }
    }

    // Draw players
    const otherPlayers = getOtherPlayers(state.players);
    const player = getPlayer(state.players);

    for (let i = 0; i < otherPlayers.length; i++) {
        const otherPlayer = otherPlayers[i];
        ctx.fillStyle = otherPlayer.isAlive ? "#F03A47" : "#A9A9A9";
        drawBlock(otherPlayer.col, otherPlayer.row);
    }

    ctx.fillStyle = player.isAlive ? "#37FF8B" : "#A9A9A9";
    drawBlock(player.col, player.row);

    // Draw bombs
    for (let i = 0; i < state.bombs.length; i++) {
        const bomb = state.bombs[i];
        ctx.fillStyle = "blue";
        drawCircle(bomb.col, bomb.row);
    }

    // Draw explosions
    for (let i = 0; i < state.explosions.length; i++) {
        const explosion = state.explosions[i];
        ctx.fillStyle = "#8b0000";
        drawBlock(explosion.col, explosion.row);
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

    for (let i = 0; i < players.length; i++) {
        if (players[i].name !== playerName) {
            arr.push(players[i]);
        }
    }

    return arr;
}

function getPlayer(players) {
    for (let i = 0; i < players.length; i++) {
        if (players[i].name === playerName) {
            return players[i];
        }
    }

    return null;
}

function createPlayerNamesMessage(playerNames) {
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

    return message;
}

function drawBlock(col, row) {
    ctx.fillRect(col * DrawSize, row * DrawSize, DrawSize, DrawSize);
}

function drawCircle(col, row) {
    ctx.beginPath();
    ctx.arc(col * DrawSize + Radius, row * DrawSize + Radius, Radius, 0, 2 * Math.PI);
    ctx.fill();
}