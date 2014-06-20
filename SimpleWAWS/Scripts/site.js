
var Template = (function () {
    function Template(json) {
        this.name = json.name;
        this.fileName = json.fileName;
        this.language = json.language;
    }
    Template.prototype.select = function (event) {
        $(".templates .btn").removeClass("active");
        $(event.target).addClass("active");
        viewModel.selectedTemplate(this);
    };
    return Template;
})();

var Site = (function () {
    function Site() {
    }
    return Site;
})();

var viewModel;

function initViewModel() {
    viewModel = this;
    viewModel.siteJson = ko.observable();
    viewModel.selectedLanguage = ko.observable();
    viewModel.selectedTemplate = ko.observable();
    viewModel.templates = ko.observableArray();
    viewModel.languages = ko.computed(function () {
        var languages = ko.utils.arrayMap(viewModel.templates(), function (item) {
            return item.language;
        });
        return ko.utils.arrayGetDistinctValues(languages).sort();
    });
    viewModel.selectLanguage = function (event) {
        $(".languages .btn").removeClass("active");
        $(event.target).addClass("active");
        viewModel.selectedLanguage($(event.target).text());
        $(".templates .btn").first().click();
    };
    ko.applyBindings(viewModel);
}
;

function initTemplates() {
    $.getJSON("/api/templates", function (data) {
        for (var i = 0; i < data.length; i++) {
            viewModel.templates.push(new Template(data[i]));
        }
    }).done(function () {
        $(".languages .btn").first().click();
    });
}

function initSite() {
    $("#loading").show();
    $.getJSON("/api/site", function (data) {
        if (data != null) {
            viewModel.siteJson(data);
            startCountDown(viewModel.siteJson().timeLeftString);
        } else {
            viewModel.siteJson(undefined);
        }
        $("#loading").hide();
    });
}

var currentTimeout = 0;
function startCountDown(init) {
    if (init !== undefined) {
        var reg = '(\\d+)(m)?(:)(\\d+)(s)?';
        var pattern = new RegExp(reg, "i");
        var match = pattern.exec(init);
        countDown(parseInt(match[1]), parseInt(match[4]));
    }
}

function countDown(minutes, seconds) {
    var siteJson = viewModel.siteJson();
    if (siteJson != undefined) {
        siteJson.timeLeftString = minutes + "m:" + ("0" + seconds).slice(-2) + "s";
        viewModel.siteJson(siteJson);
        seconds--;
        if (seconds === -1 && minutes !== 0) {
            seconds = 59;
            minutes--;
        } else if (seconds === -1 && minutes === 0) {
            $(".site-info-valid").removeClass("site-info-valid").addClass("site-info-not-valid");
            siteJson.url = "http://azure.microsoft.com/en-us/pricing/free-trial/";
            siteJson.monacoUrl = "http://azure.microsoft.com/en-us/pricing/free-trial/";
            siteJson.kuduConsoleWithCreds = "http://azure.microsoft.com/en-us/pricing/free-trial/";
            siteJson.contentDownloadUrl = "http://azure.microsoft.com/en-us/pricing/free-trial/";
            viewModel.siteJson(siteJson);
            return;
        } else if (minutes === 0 && !$(".countdown").hasClass("site-info-not-valid")) {
            $(".countdown").addClass("site-info-not-valid");
        }
        if (minutes === 0 && seconds <= 10 && !$(".countdown").hasClass("slow")) {
            $(".countdown").addClass("slow");
        }
        currentTimeout = setTimeout(countDown, 1000, minutes, seconds);
    }
}

window.onload = function () {
    initViewModel();
    initTemplates();
    initSite();
    $("#create-site").click(function () {
        $("#loading").show();
       $.ajax({
            type: "POST",
            url: "/api/site",
            data: JSON.stringify(viewModel.selectedTemplate()),
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                viewModel.siteJson(data);
                startCountDown(viewModel.siteJson().timeLeftString);
                $("#loading").hide();
            }
        });
    });
};