var towserMouseInit = function (self) {
    var on = function (el, type, handler, capture) {
        el.addEventListener(type, handler, capture || false);
    }

    var off = function (el, type, handler, capture) {
        el.removeEventListener(type, handler, capture || false);
    }

    var cancel = function (ev) {
        if (ev.preventDefault) ev.preventDefault();
        ev.returnValue = false;
        if (ev.stopPropagation) ev.stopPropagation();
        ev.cancelBubble = true;
        return false;
    }

    var sendButton = function (ev) {
        // get the xterm-style button
        var button = getButton(ev);
        if (button < 0) { return false; }

        // get mouse coordinates
        var pos = getCoords(ev);
        if (!pos) { return false; }

        sendEvent(button, pos);
        return true;
    }

    // send SBClient mouse event:
    var sendEvent = function (button, pos) {
        var data = "\x1b[M" + button + ";" + pos.x + ";" + pos.y + "\r";
        if (term.oninput) { term.oninput(data); }
    }

    var getButton = function (ev) {
        var button;

        if (ev.type === "mousedown" && ev.button === 0) {
            // 1 = left
            button = 1;
        } else if (ev.type === "mousedown" && ev.button === 1) {
            // 2 = middle
            button = 2;
        } else if (ev.type === "mouseup") {
            // 0 = release
            button = 0;
        } else {
            // -1 = ignore
            button = -1;
        }
        return button;
    }

    // mouse coordinates measured in cols/rows
    var getCoords = function (ev) {
        var x, y, w, h, el;

        // ignore browsers without pageX for now
        if (ev.pageX == null) return;

        x = ev.pageX;
        y = ev.pageY;
        el = self.element;

        // should probably check offsetParent
        // but this is more portable
        while (el && el !== self.document.documentElement) {
            x -= el.offsetLeft;
            y -= el.offsetTop;
            el = 'offsetParent' in el
              ? el.offsetParent
              : el.parentNode;
        }

        // convert to cols/rows
        w = self.element.clientWidth;
        h = self.element.clientHeight;
        x = Math.floor((x / w) * self.cols);
        y = Math.floor((y / h) * self.rows);

        // be sure to avoid sending
        // bad positions to the program
        if (x < 0) x = 0;
        if (x >= self.cols) x = self.cols - 1;
        if (y < 0) y = 0;
        if (y >= self.rows) y = self.rows - 1;

        return {
            x: x,
            y: y,
        };
    }

    var mouseIsDown = false;

    var bindMouse = function () {
        on(self.element, 'mousedown', function (ev) {
            if (!self.mouseEvents) return;

            // ensure focus
            self.focus();

            // send the button
            if (sendButton(ev)) {
                mouseIsDown = true;
                return cancel(ev);
            }
        });

        on(self.document, 'mouseup', function up(ev) {
            if (!mouseIsDown) { return; }
            if (sendButton(ev)) {
                mouseIsDown = false;
                return cancel(ev);
            }
        });
    };

    self.bindMouse = bindMouse;
};