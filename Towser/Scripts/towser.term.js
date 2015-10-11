var towserTermInit = function () {
    var term = Terminal.towserTerminal;

    if (term) {
        if (term.onstop) { term.onstop(); }
        term.onstop = null;
        term.oninput = null;

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

        term.open();

        Terminal.towserTerminal = term;
    }

    return term;
}