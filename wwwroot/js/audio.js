const MaximumAudioChannels = 50;
let audioChannels = [];

for (let i = 0; i < MaximumAudioChannels; i++) {
	audioChannels[i] = [];
	audioChannels[i]['identifier'] = "";
	audioChannels[i]['channel'] = new Audio();
	audioChannels[i]['finished'] = -1;
}

function playSound(audioId) {
	for (let i = 0; i < MaximumAudioChannels; i++) {
		currentTime = new Date();

		if (audioChannels[i]['finished'] < currentTime.getTime()) {
			const audioElement = document.getElementById(audioId);

			audioChannels[i]['finished'] = currentTime.getTime() + audioElement.duration * 1000;
			audioChannels[i]['channel'].src = audioElement.src;
			audioChannels[i]['channel'].load();
			audioChannels[i]['channel'].play();

			// Used to stop sound for this user if needed later
			audioChannels[i]['identifier'] = audioId;
			break;
		}
	}
}

function stopSound(audioId) {
	for (let i = 0; i < MaximumAudioChannels; i++) {
		// Pause specific sound and mark it as finished
		if (audioChannels[i]['identifier'] === audioId) {
			const audioElement = document.getElementById(audioId);
			audioElement.pause();
			audioChannels[i]['finished'] = -1;
		}
	}
}