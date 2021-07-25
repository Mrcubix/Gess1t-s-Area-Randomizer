var fullAreaContainer;
var definedAreaContainer;
var targetAreaContainer;
var penPositionContainer;

var fullArea;
var definedArea;
var targetArea;
var penPosition;

var area;

document.addEventListener('DOMContentLoaded', async function () {   
    fullAreaContainer = document.getElementsByClassName("FullAreaContainer")[0];
    definedAreaContainer = document.getElementsByClassName("DefinedAreaContainer")[0];
    targetAreaContainer = document.getElementsByClassName("TargetAreaContainer")[0];
    penPositionContainer = document.getElementsByClassName("PenPositionContainer")[0];

    fullArea = document.getElementById("FullArea");
    definedArea = document.getElementById("DefinedArea");
    targetArea = document.getElementById("TargetArea");
    penPosition = document.getElementById("PenPosition");

    let request = await fetch("/SocketPort");
    let text = await request.text();
    let port = parseInt(text);

    let webSocketURL = "ws://localhost:"+port;

    var socket = new WebSocket(webSocketURL);

    socket.onopen = function (event) {
        console.log("Connected, send request now...");
        socket.send('{"id": "AreaRandomizer"}');
        console.log("Initial Query sent");
    };
    socket.onmessage = function (message) {
        var json = JSON.parse(message.data);
        if (json.Area != undefined) {
            setArea(json.Area);
        }
        if (json.TargetArea != undefined) {
            setTargetArea(json.TargetArea);
            area = json["TargetArea"]["fullArea"];
            lpmm = json["TargetArea"]["lpmm"];
            SetFullArea(json["TargetArea"]["fullArea"]);
        }
        if (json.Position != undefined) {

            SetPos(json.Position);
        }
    }
})
function SetPos(pos) {
    if (area != undefined) {
        penPosition.setAttribute("cx", String(pos.X)+"mm");
        penPosition.setAttribute("cy", String(pos.Y)+"mm");
    }
}

function SetFullArea(area) {
    if (area != undefined) {
        fullAreaContainer.setAttribute("width", String(area.X)+"mm");
        fullAreaContainer.setAttribute("height", String(area.Y)+"mm");

        definedAreaContainer.setAttribute("height", String(area.Y)+"mm");
        definedAreaContainer.setAttribute("height", String(area.Y)+"mm");

        targetAreaContainer.setAttribute("height", String(area.Y)+"mm");
        targetAreaContainer.setAttribute("height", String(area.Y)+"mm");

        penPositionContainer.setAttribute("width", String(area.X)+"mm");
        penPositionContainer.setAttribute("height", String(area.Y)+"mm");

        fullArea.setAttribute("width", String(area.X)+"mm");
        fullArea.setAttribute("height", String(area.Y)+"mm");
        fullArea.setAttribute("x", "0mm");
        fullArea.setAttribute("y", "0mm");
    }
}

function setArea(area) {
    if (area != undefined) {
        definedArea.setAttribute("width", String(area.size.X)+"mm");
        definedArea.setAttribute("height", String(area.size.Y)+"mm");
        definedArea.setAttribute("x", String(area.position.X - (area.size.X / 2))+"mm");
        definedArea.setAttribute("y", String(area.position.Y - (area.size.Y / 2))+"mm");
    }
}

function setTargetArea(area) {
    if (area != undefined) {
        targetArea.setAttribute("width", String(area.size.X)+"mm");
        targetArea.setAttribute("height", String(area.size.Y)+"mm");
        targetArea.setAttribute("x", String(area.position.X - (area.size.X / 2))+"mm");
        targetArea.setAttribute("y", String(area.position.Y - (area.size.Y / 2))+"mm");
    }
}