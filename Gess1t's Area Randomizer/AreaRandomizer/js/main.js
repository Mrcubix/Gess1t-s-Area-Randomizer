var fullAreaContainer;
var definedAreaContainer;
var targetAreaContainer;
var penPositionContainer;

var fullArea;
var definedArea;
var targetArea;
var penPosition;

document.addEventListener('DOMContentLoaded', async function () {   
    fullAreaContainer = document.getElementsByClassName("FullAreaContainer")[0];
    definedAreaContainer = document.getElementsByClassName("definedAreaContainer")[0];
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
        socket.send('{"pipe": "AreaRandomizer"}');
        console.log("Initial Query sent");
    };
    socket.onmessage = function (message) {
        var json = JSON.parse(message.data);
        if (json.GetFullArea != undefined) {
            SetFullArea(json.GetFullArea);
        }
        if (json.GetAreaAsync != undefined) {
            setArea(json.GetAreaAsync);
        }
        if (json.GetTargetAreaAsync != undefined) {
            setTargetArea(json.GetTargetAreaAsync);
        }
        if (json.GetPosition != undefined) {
            SetPos(json.GetPosition);
        }
        console.log(json)
    }
})
function SetPos(pos) {
    if (area != undefined) {
        penCursorPositionCircle.setAttribute("cx", String(pos.X / area.lpmm)+"mm");
        penCursorPositionCircle.setAttribute("cy", String(pos.Y / area.lpmm)+"mm");
    }
}

function SetFullArea(area) {
    if (area != undefined) {
        tabletAreaContainer.setAttribute("width", String(area.FullArea.size.X)+"mm");
        tabletAreaContainer.setAttribute("height", String(area.FullArea.size.Y)+"mm");

        userDefinedAreaContainer.setAttribute("height", String(area.FullArea.size.Y)+"mm");
        userDefinedAreaContainer.setAttribute("height", String(area.FullArea.size.Y)+"mm");

        penPositionContainer.setAttribute("width", String(area.FullArea.size.X)+"mm");
        penPositionContainer.setAttribute("height", String(area.FullArea.size.Y)+"mm");

        fullArea.setAttribute("width", String(area.FullArea.size.X)+"mm");
        fullArea.setAttribute("height", String(area.FullArea.size.Y)+"mm");
        fullArea.setAttribute("x", "0mm");
        fullArea.setAttribute("y", "0mm");
    }
}

function setArea(area) {
    if (area != undefined) {
        userDefinedArea.setAttribute("width", String(area.size.X)+"mm");
        userDefinedArea.setAttribute("height", String(area.size.Y)+"mm");
        userDefinedArea.setAttribute("x", String(area.position.X - (area.size.X / 2))+"mm");
        userDefinedArea.setAttribute("y", String(area.position.Y - (area.size.Y / 2))+"mm");
    }
}

function setTargetArea(area) {
    if (area != undefined) {

    }
}