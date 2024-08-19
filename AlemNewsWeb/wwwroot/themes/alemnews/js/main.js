document.addEventListener("DOMContentLoaded", function () {

    if (document.querySelectorAll("a[data-language]")) {
        document.querySelectorAll("a[data-language]").forEach((a) => {
            a.addEventListener("click", function (event) {
                event.preventDefault();
                let aNode = this,
                    pathName = window.location.pathname,
                    currentLanguge = $qar.getCurrentLanguage(),
                    dataLanguage = aNode.getAttribute("data-language")
                        ? aNode.getAttribute("data-language").toLowerCase()
                        : "";
                if (currentLanguge === dataLanguage) return;
                if (!pathName) {
                    window.location.href = "/" + dataLanguage + window.location.search;
                }
                // if (pathName && pathName.length < currentLanguge.length) return;
                window.location.href =
                    "/" +
                    dataLanguage +
                    pathName.substring(currentLanguge.length + 1) +
                    window.location.search;
            });
        });
    }
    const lightsunElements = document.querySelectorAll('.light-sun');
    const darkmoonElements = document.querySelectorAll('.dark-moon');
    const showbacktop = document.querySelector('#showbacktop');
    const navmenu = showbacktop.querySelector('#main-menu');

    function updateThemeClasses(theme) {
        if (theme === 'dark') {
            showbacktop.classList.add('bg-black');
            showbacktop.classList.remove('bg-white');
            navmenu.classList.add('navbar-dark');
            navmenu.classList.remove('navbar-light');
        } else {
            showbacktop.classList.add('bg-white');
            showbacktop.classList.remove('bg-black');
            navmenu.classList.add('navbar-light');
            navmenu.classList.remove('navbar-dark');
        }
    }

    // Check the saved theme from localStorage and apply it
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme) {
        document.documentElement.setAttribute('data-bs-theme', savedTheme);
        updateThemeClasses(savedTheme);

        if (savedTheme === 'dark') {
            lightsunElements.forEach(lightsun => lightsun.classList.remove('d-none'));
            darkmoonElements.forEach(darkmoon => darkmoon.classList.add('d-none'));
        } else {
            lightsunElements.forEach(lightsun => lightsun.classList.add('d-none'));
            darkmoonElements.forEach(darkmoon => darkmoon.classList.remove('d-none'));
        }
    }

    if (lightsunElements.length && darkmoonElements.length) {
        lightsunElements.forEach((lightsun, index) => {
            lightsun.addEventListener("click", function (event) {
                event.preventDefault();
                const theme = 'light';
                document.documentElement.setAttribute('data-bs-theme', theme);
                localStorage.setItem('theme', theme);

                lightsun.classList.add("d-none");
                if (darkmoonElements[index]) {
                    darkmoonElements[index].classList.remove("d-none");
                }

                updateThemeClasses(theme);
            });
        });

        darkmoonElements.forEach((darkmoon, index) => {
            darkmoon.addEventListener("click", function (event) {
                event.preventDefault();
                const theme = 'dark';
                document.documentElement.setAttribute('data-bs-theme', theme);
                localStorage.setItem('theme', theme);

                darkmoon.classList.add("d-none");
                if (lightsunElements[index]) {
                    lightsunElements[index].classList.remove("d-none");
                }

                updateThemeClasses(theme);
            });
        });
    }
    document.body.addEventListener("submit", (e) => {
        const that = e.target;

        if (that.classList.contains("uly-search")) {
            e.preventDefault();
            const keyword = that.querySelector("input.uly-input-search").value,
                url = that.getAttribute("action");
            window.location.href = url + `?keyword=${keyword}`;
        }
    });

    const newsTitles = document.querySelectorAll('.news-title');

    newsTitles.forEach(function (title) {
        if (title.textContent.length > 40) {
            title.textContent = title.textContent.substring(0, 40) + '...';
        }
    });
    $(function () {
        // Initializing when the document is ready
        $('button.btn-play-speech').each(function () {
            let $btn = $(this);
            let containerDiv = $btn.closest('div.article-view');
            let combinedText = '';
            containerDiv.find('.read-text').each(function () {
                combinedText += $(this).text() + ' ';
            });
            combinedText = combinedText.trim();
            $btn.attr('data-text', combinedText);
        });

        // Handling click event for all buttons
        $('button.btn-play-speech').click(function () {
            let $btn = $(this),
                $icon = $btn.find('i'),
                requestData = {
                    text: $btn.attr('data-text')
                };

            $btn.prop("disabled", true);
            $icon.addClass('fa-fade');

            $.ajax({
                type: "POST",
                url: "/" + $qar.getCurrentLanguage() + "/Home/TextToSpeech",
                contentType: "application/json",
                data: JSON.stringify(requestData),
                success: function (response, status, xhr) {
                    var blob = new Blob([response], { type: "audio/wav" });
                    var url = window.URL.createObjectURL(blob);
                    var audio = new Audio();
                    audio.src = url;
                    audio.play()
                        .then(() => {
                        })
                        .catch(e => {
                            console.error("Error playing audio: ", e);
                            $btn.prop("disabled", false);
                            $icon.removeClass('fa-fade');
                        });
                    audio.onended = function () {
                        $btn.prop("disabled", false);
                        $icon.removeClass('fa-fade');
                    };
                },
                error: function (xhr, status, error) {
                    console.error("Error: " + error);
                    $btn.prop("disabled", false);
                    $icon.removeClass('fa-fade');
                },
                xhrFields: {
                    responseType: 'blob'
                }
            });
        });
    });
    var box = document.getElementById("box");
    var box1 = document.getElementById("box1");
    function myScroll() {
        if (box.scrollLeft - box1.scrollWidth <= 0) {
            box.scrollLeft++;
        } else {
            box.scrollLeft = 0;
        }
    }

    var speed = 30; 
    var MyMar = setInterval(myScroll, speed);

    box.onmouseover = function () {
        clearInterval(MyMar);
    };

    box.onmouseout = function () {
        MyMar = setInterval(myScroll, speed);
    };

});
