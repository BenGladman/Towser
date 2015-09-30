var bridgInit = function () {
    var term = new Terminal({
        cols: 132,
        rows: 24,
        screenKeys: true,
        cursorBlink: false,
        colors: Terminal.tangoColors,
        parent: document.getElementById("terminal-container")
    });

    //override term.js keyboard & mouse handling
    keyboardInit(term);
    mouseInit(term);

    term.open();

    var connection = $.connection('/bridg');

    // receive from signalR
    connection.received(function (data) {
        term.write(data);
    });

    connection.start().done(function () {
        console.log('Now connected');

        // receive from terminal
        term.on("data", function (data) {
            connection.send(data);
        });
    });

    window.document.getElementById("buttons").style.display = "none";
}