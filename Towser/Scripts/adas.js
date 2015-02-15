var adasInit = function () {
    var term = new Terminal({
        cols: 132,
        rows: 24,
        useStyle: true,
        screenKeys: true,
        cursorBlink: false
    });

    //override term.js keyboard handling
    var emitFunction = function (ch) { term.emit("data", ch); };
    var onKeyHandler = keyboardInit(emitFunction);
    term.keyDown = onKeyHandler;
    term.keyPress = onKeyHandler;

    var move = function (movemode, moverow, movecol) {
        switch (movemode) {
            case 1:
                // row and col
                term.cursorPos([moverow + 1, movecol + 1]);
                break;
            case 2:
                // row
                term.cursorPos([moverow + 1, term.x]);
                break;
            case 3:
                // relative row
                term.cursorPos([moverow + term.y, term.x]);
                break;
            case 4:
                // col
                term.move([term.y, movecol + 1]);
                break;
            case 5:
                // relative col
                term.move([term.y, movecol + term.x]);
                break;
        }
    };

    var clear = function (clearmode) {
        switch (clearmode) {
            case 1: term.eraseInDisplay([2]); break;
            case 2: term.eraseInLine([0]); break;
            case 3: term.eraseInDisplay([0]); break;
        }
    };

    var processFragment = function (fragment) {
        if (fragment.t) { term.write(fragment.t); }
        move(fragment.m, fragment.mr || 0, fragment.mc || 0);
        clear(fragment.c);
        if (fragment.sgr) { term.charAttributes(fragment.sgr); }
        term.refresh(term.refreshStart, term.refreshEnd);
    };

    term.open(document.body);

    var hub = $.connection.adasHub;

    hub.client.write = function (data) {
        data.forEach(processFragment);
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
    window.term = term;
}