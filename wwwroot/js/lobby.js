"use strict";

/////////////
// Startup //
/////////////

// Attempt to set up the SignalR connection to the lobby hub.
const lobbyConnection = new signalR.HubConnectionBuilder().withUrl("/lobbyHub").build();

document.addEventListener("DOMContentLoaded", function (event) {
    // Disable room buttons until connection is established.
    document.getElementById("joinRoomButton").disabled = true;
    document.getElementById("createRoomButton").disabled = true;

    // If cookies are enabled and the user has previously created a player name, place it back into the input.
    if (areCookiesEnabled()) {
        const previousPlayerName = readCookie("playerName");

        if (previousPlayerName != null) {
            document.getElementById("playerNameInput").value = previousPlayerName;
        }
    }
});

////////////////////////////////////////////////////////////////
// Event Handlers for the SignalR hub connection of the lobby //
////////////////////////////////////////////////////////////////

// Upon successful connection to the hub, enable room buttons.
lobbyConnection.start().then(function () {
    document.getElementById("joinRoomButton").disabled = false;
    document.getElementById("createRoomButton").disabled = false;
}).catch(function (err) {
    return handleError(err);
});

// Parses the model for room information.
// If the room does not exist in the table, it creates a table row for this model.
// Otherwise, it updates the associated table row with this model.
// If there are no players in the room, the entire row is removed.
// Hides the "no rooms" placeholder.
lobbyConnection.on("UpdateRoomInTable", function (room) {
    console.log("Updating room " + room + " with players " + room.playerNames.join(", "));
    const roomName = room.roomName;
    const playerNames = room.playerNames;

    let roomRow = document.getElementById(roomName);
    let roomNameTd = document.getElementById(roomName + "-roomName");
    let playerNamesTd = document.getElementById(roomName + "-playerNames");

    if (roomRow == null) {
        roomRow = document.createElement("tr");
        roomRow.id = roomName;

        // Update room name table data
        roomNameTd = document.createElement("td");
        roomNameTd.id = roomName + "-roomName";
        roomNameTd.textContent = roomName;
        roomNameTd.classList.add("col-6");

        // Update display name table data
        playerNamesTd = document.createElement("td");
        playerNamesTd.id = roomName + "-playerNames";
        playerNamesTd.classList.add("col-6");

        // Append updated row onto table body
        roomRow.appendChild(roomNameTd);
        roomRow.appendChild(playerNamesTd);
        document.getElementById("roomTableBody").appendChild(roomRow);
    }

    if (playerNames.length > 0) {
        playerNamesTd.textContent = playerNames.join(", ");
    }
    else {
        roomRow.remove();
    }

    // Hide "no rooms" placeholders if there is a room, and unhide it if it is.
    // TODO: For now, this should always hide. On the remove room handler, hide if roomCount is 0.
    const placeholders = document.getElementsByClassName("no-rooms-placeholder");
    const roomCount = document.getElementById("roomTableBody").childElementCount - 2;

    for (let i = 0; i < placeholders.length; i++) {
        placeholders[i].hidden = (roomCount > 0);
    }
});

//////////////////////////////////////////
// Event Handlers for document elements //
//////////////////////////////////////////

// Validates the form for the room name and player name.
// Creates a model for these values and submits an event to the hub.
// This event signals to add the specified player to the specified room.
document.getElementById("joinRoomButton").addEventListener("click", function (event) {
    let isValidForm = true;

    if (isInputEmpty("joinRoomInput")) {
        markAsInvalid("joinRoomInput");
        isValidForm = false;
    }

    if (isInputEmpty("playerNameInput")) {
        markAsInvalid("playerNameInput");
        isValidForm = false;
    }

    if (!isValidForm) {
        event.preventDefault();
        event.stopPropagation();
        return;
    }

    const roomName = document.getElementById("joinRoomInput").value;
    const playerName = document.getElementById("playerNameInput").value;

    validateRedirectToRoom(roomName, playerName);

    event.preventDefault();
});

// Validates the form for the player name.
// Creates a model for these values and submits chained events to the hub.
// The first event creates a room and persists that room name.
// If the first event was successful, the second event adds the player to the newly created room.
document.getElementById("createRoomButton").addEventListener("click", function (event) {
    if (isInputEmpty("playerNameInput")) {
        markAsInvalid(event.target.id);
        event.preventDefault();
        event.stopPropagation();
        return;
    }

    const playerName = document.getElementById("playerNameInput").value;

    lobbyConnection.invoke("GenerateRoomName", { playerName }).then(function (roomName) {
        validateRedirectToRoom(roomName, playerName);
    }).catch(function (err) {
        return handleError(err);
    });

    event.preventDefault();
});

// Unmarks the "join room" input form as invalid.
document.getElementById("joinRoomInput").addEventListener("click", function (event) {
    unmarkAsInvalid(event.target.id);
});

// Unmarks the "player name" input form as invalid.
document.getElementById("playerNameInput").addEventListener("click", function (event) {
    unmarkAsInvalid(event.target.id);
});

// Restricts valid inputs for player name
document.getElementById('playerNameInput').addEventListener('keydown', function (event) {
    const maxLength = 12;

    if (event.code === 'Enter' || event.code === 'Space') {
        event.preventDefault();
    }
    else if (isAlphaNumeric(event.key) && this.value.length >= maxLength && !event.ctrlKey) {
        this.value = this.value.substr(0, maxLength);
        event.preventDefault();
	}

    event.stopPropagation();
});

/////////////
// Utility //
/////////////

// Checks whether cookies are enabled for the browser.
// If so, then a cookie is generated and the client is redirected without query parameters.
// Otherwise, the client is redirected with query parameters.
function validateRedirectToRoom(roomName, playerName) {
    lobbyConnection.invoke("ValidateJoin", roomName, playerName).then(function (isValidJoin) {
        if (isValidJoin) {
            if (areCookiesEnabled()) {
                createCookie("playerName", playerName, 1);
                location.replace(window.location.href + "room/" + roomName);
            }
            else {
                location.replace(window.location.href + "room/" + roomName + "?playerName=" + playerName);
            }
        }
        else {
            alert("Player " + playerName + " already exists in room " + roomName);
		}
    }).catch(function (err) {
        return handleError(err);
    });
}