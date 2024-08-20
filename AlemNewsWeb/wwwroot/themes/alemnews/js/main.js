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

    var boxes = document.querySelectorAll(".box");
    var boxes1 = document.querySelectorAll(".box1");
    const direction = document.body.getAttribute("data-pc-direction");
    console.log(direction);

    function myScroll(box, box1) {
        if (box.scrollLeft >= box1.scrollWidth / 2) {
            box.scrollLeft = 0;
        } else {
            if (direction == "rtl") {
                box.scrollLeft--;
            }
            if (direction == "ltr") {
                box.scrollLeft++;
            }

        }
    }

    var speed = 30;
    var intervals = [];
    function ensureScrollable(box1) {
        var totalWidth = 0;
        var boxChildren = box1.children;
        for (let i = 0; i < boxChildren.length; i++) {
            totalWidth += boxChildren[i].offsetWidth;
        }
        if (totalWidth < box1.offsetWidth) {
            let cloneTimes = Math.ceil(box1.offsetWidth / totalWidth);
            for (let i = 0; i < cloneTimes; i++) {
                let clone = box1.cloneNode(true);
                box1.appendChild(clone);
            }
        } else {
            let clone = box1.cloneNode(true);
            box1.appendChild(clone);
        }
    }
    boxes.forEach((box, index) => {
        var box1 = boxes1[index];
        ensureScrollable(box1);

        var MyMar = setInterval(() => myScroll(box, box1), speed);
        intervals.push(MyMar);

        box.onmouseover = function () {
            clearInterval(MyMar);
        };

        box.onmouseout = function () {
            MyMar = setInterval(() => myScroll(box, box1), speed);
        };
    });





    let adfoxbranding = document.querySelector('.adfox-banner-background'),
        maxTopPosition = 344,
        footer = document.querySelector('footer');
    window.addEventListener('scroll', () => {
        let scrollTopPosition = document.documentElement.scrollTop;
        let footerTopPosition = footer.getBoundingClientRect().top + window.scrollY;
        if (scrollTopPosition < maxTopPosition) {
            adfoxbranding.style.position = "absolute";
            adfoxbranding.style.top = "344px";
            adfoxbranding.classList.remove("mt-3 mt-md-5");
        }
        else if (scrollTopPosition >= maxTopPosition && scrollTopPosition < footerTopPosition - adfoxbranding.offsetHeight) {
            adfoxbranding.style.position = "fixed";
            adfoxbranding.style.top = "0px";
        }
        else if (scrollTopPosition >= footerTopPosition - adfoxbranding.offsetHeight) {
            adfoxbranding.style.position = "absolute";
            adfoxbranding.style.top = `${footerTopPosition - adfoxbranding.offsetHeight}px`;
        }
    });
});
