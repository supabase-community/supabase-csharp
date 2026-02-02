(function () {
    window.addEventListener("load", () => {
        let throttlePause;
        const throttle = (callback, time) => {
            if (throttlePause) return;
            throttlePause = true;

            setTimeout(() => {
                callback();
                throttlePause = false;
            }, time);
        };

        var serializeEvent = function (e) {
            if (e) {
                var o = {
                    altKey: e.altKey,
                    buttons: e.buttons,
                    ctrlKey: e.ctrlKey,
                    metaKey: e.metaKey,
                    shiftKey: e.shiftKey,
                    code: e.code,
                    key: e.key,
                    keyCode: e.keyCode
                };
                return o;
            }
        };

        window.registerMouseMoveListener = (dotnetHelper) => {
            document.addEventListener('mousemove', (ev) => {
                throttle(() => {
                    dotnetHelper.invokeMethodAsync('HandleMouseMoved', [ev.clientX, ev.clientY])
                }, 50)
            })
        }

        window.registerKeydownListener = (dotnetHelper) => {
            document.addEventListener('keydown', (ev) => {
                dotnetHelper.invokeMethodAsync('HandleKeydown', serializeEvent(ev))
            })
        }
    })
})()