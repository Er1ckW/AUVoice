let socket = null;

const elements = {
    status: document.getElementById('connection-status'),
    url: document.getElementById('ws-url'),
    connectBtn: document.getElementById('connect-btn'),
    disconnectBtn: document.getElementById('disconnect-btn'),
    gameState: document.getElementById('game-state'),
    gameCode: document.getElementById('game-code'),
    mapId: document.getElementById('map-id'),
    comms: document.getElementById('comms-status'),
    playerCount: document.getElementById('player-count'),
    playersTable: document.getElementById('players-table').querySelector('tbody'),
    jsonOutput: document.getElementById('json-output')
};

function updateStatus(connected) {
    if (connected) {
        elements.status.textContent = 'Connected';
        elements.status.className = 'status connected';
        elements.connectBtn.disabled = true;
        elements.disconnectBtn.disabled = false;
    } else {
        elements.status.textContent = 'Disconnected';
        elements.status.className = 'status disconnected';
        elements.connectBtn.disabled = false;
        elements.disconnectBtn.disabled = true;
    }
}

function connect() {
    const url = elements.url.value;
    try {
        socket = new WebSocket(url);
        
        socket.onopen = () => {
            console.log('Connected to', url);
            updateStatus(true);
        };

        socket.onmessage = (event) => {
            try {
                const data = JSON.parse(event.data);
                updateUI(data);
            } catch (err) {
                console.error('Error parsing JSON:', err);
            }
        };

        socket.onclose = () => {
            console.log('Disconnected');
            updateStatus(false);
        };

        socket.onerror = (err) => {
            console.error('WebSocket error:', err);
            updateStatus(false);
        };
    } catch (err) {
        alert('Invalid URL or connection failed');
    }
}

function disconnect() {
    if (socket) {
        socket.close();
    }
}

function updateUI(data) {
    // Update basic info
    elements.gameState.textContent = data.gameState || 'UNKNOWN';
    elements.gameCode.textContent = data.gameCode || '-';
    elements.mapId.textContent = data.mapId !== undefined ? data.mapId : '-';
    elements.comms.textContent = data.gameData?.commsSabotaged ? 'SABOTAGED' : 'NORMAL';
    
    // Update raw JSON
    elements.jsonOutput.textContent = JSON.stringify(data, null, 2);

    // Update players
    const players = data.players || [];
    elements.playerCount.textContent = players.length;
    
    elements.playersTable.innerHTML = '';
    players.forEach(p => {
        const row = document.createElement('tr');
        if (p.isDead) row.className = 'is-dead';
        
        const roleClass = p.isImpostor ? 'role-impostor' : 'role-crewmate';
        const roleText = p.isImpostor ? 'Impostor' : 'Crewmate';

        row.innerHTML = `
            <td>${p.id}</td>
            <td>${p.name}</td>
            <td class="${roleClass}">${roleText}</td>
            <td>${p.isDead ? '💀 Dead' : '💖 Alive'}${p.disconnected ? ' (DC)' : ''}</td>
            <td>${p.position ? `${p.position.x.toFixed(2)}, ${p.position.y.toFixed(2)}` : 'N/A'}</td>
            <td>${p.isLocal ? '✅' : ''}</td>
        `;
        elements.playersTable.appendChild(row);
    });
}

elements.connectBtn.addEventListener('click', connect);
elements.disconnectBtn.addEventListener('click', disconnect);
