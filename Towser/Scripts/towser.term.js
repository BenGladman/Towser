var towserTermInit = function () {
    var term = Terminal.towserTerminal;

    if (term) {
        if (term.onreset) { term.onreset(); }
        term.onreset = null;

    } else {
        term = new Terminal({
            cols: 132,
            rows: 24,
            screenKeys: true,
            cursorBlink: false,
            colors: Terminal.tangoColors,
            parent: document.getElementById("terminal-container")
        });

        //override term.js keyboard & mouse handling
        towserKeyboardInit(term);
        towserMouseInit(term);

        // receive from terminal
        term.on("data", function (data) {
            if (term.ondata) { term.ondata(data); }
        });

        term.open();

        Terminal.towserTerminal = term;
    }

    return term;
}