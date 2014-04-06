var Timer = (function () {
    function Timer() {
        this._counter = {
            value: 0
        };
        this._timerId = null;
    }
    Object.defineProperty(Timer.prototype, "counter", {
        get: function () {
            return this._counter;
        },
        enumerable: true,
        configurable: true
    });

    Timer.prototype.toggleInterval = function () {
        var _this = this;
        if (this._timerId == null) {
            // Start the timer
            this._timerId = setInterval(function () {
                _this._counter.value++;
                if (_this._counter.value > 1000)
                    _this._counter.value = 0;
                console.log(_this._counter.value);
            }, 100);
        } else {
            // Stop the timer
            clearInterval(this._timerId);
            this._timerId = null;
        }
    };
    return Timer;
})();
