﻿var adacInit = function () {
    var term = new Terminal({
        cols: 132,
        rows: 24,
        useStyle: true,
        screenKeys: true,
        cursorBlink: false,
        colors: Terminal.xtermColors
    });

    //override term.js keyboard & mouse handling
    keyboardInit(term);
    mouseInit(term);

    term.open(document.body);

    var hub = $.connection.adacHub;

    hub.client.write = function (data) {
        term.write(data);
    }

    $.connection.hub.start()
        .done(function () {
            console.log('Now connected, connection ID=' + $.connection.hub.id);

            // receive from terminal
            term.on("data", function (data) {
                hub.server.keyPress(data);
            });
        })

        .fail(function () {
            console.log('Could not Connect!');
        });

    window.document.getElementById("buttons").style.display = "none";
}