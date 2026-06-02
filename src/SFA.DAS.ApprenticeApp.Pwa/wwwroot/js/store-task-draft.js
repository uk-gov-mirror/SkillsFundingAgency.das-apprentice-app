(function () {
    var form = document.getElementById('edit-task-form');
    var taskIdField = document.getElementById('TaskId');

    if (!form || !taskIdField || !taskIdField.value) {
        return;
    }

    var storageKey = 'edit-task-draft:' + taskIdField.value;

    function getKey(el) {
        return el.id || el.name || '';
    }

    function saveDraft() {
        var state = {};

        Array.prototype.forEach.call(form.elements, function (el) {
            if (!el || el.disabled) return;

            var key = getKey(el);
            if (!key) return;

            if (el.type === 'file') {
                return;
            }

            if (el.type === 'checkbox') {
                state[key] = {
                    type: 'checkbox',
                    checked: el.checked,
                    value: el.value
                };
                return;
            }

            if (el.type === 'radio') {
                if (el.checked) {
                    state[el.name] = {
                        type: 'radio',
                        value: el.value
                    };
                }
                return;
            }

            if (el.tagName === 'SELECT' && el.multiple) {
                state[key] = {
                    type: 'select-multiple',
                    value: Array.prototype.map.call(el.selectedOptions, function (option) {
                        return option.value;
                    })
                };
                return;
            }

            state[key] = {
                type: 'value',
                value: el.value
            };
        });

        sessionStorage.setItem(storageKey, JSON.stringify(state));
    }

    function restoreDraft() {
        var saved = sessionStorage.getItem(storageKey);
        if (!saved) return;

        var state;
        try {
            state = JSON.parse(saved);
        } catch (e) {
            return;
        }

        Array.prototype.forEach.call(form.elements, function (el) {
            if (!el || el.disabled) return;

            var key = getKey(el);
            if (!key) return;

            var savedEntry = state[key] || state[el.name];
            if (!savedEntry) return;

            if (el.type === 'file') {
                return;
            }

            if (el.type === 'checkbox') {
                if (typeof savedEntry.checked === 'boolean') {
                    el.checked = savedEntry.checked;
                }
                return;
            }

            if (el.type === 'radio') {
                el.checked = savedEntry.value === el.value;
                return;
            }

            if (el.tagName === 'SELECT' && el.multiple && Array.isArray(savedEntry.value)) {
                Array.prototype.forEach.call(el.options, function (option) {
                    option.selected = savedEntry.value.indexOf(option.value) !== -1;
                });
                return;
            }

            if (savedEntry.value !== undefined && savedEntry.value !== null) {
                el.value = savedEntry.value;
            }
        });
    }

    restoreDraft();

    document.addEventListener('input', saveDraft, true);
    document.addEventListener('change', saveDraft, true);
    window.addEventListener('beforeunload', saveDraft);

    document.addEventListener('click', function (e) {
        var link = e.target.closest('button');
        if (!link) return;

        var href = link.getAttribute('href') || '';
        if (href === '#' || link.classList.contains('js-back-link')) {
            return;
        }

        saveDraft();
    }, true);

    window.addEventListener('pageshow', function () {
        restoreDraft();
    });
})();