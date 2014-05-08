var wawsSiteCookie = "WAWSSiteId";
var idCookieValue = "Id";

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
    var cookie = $.cookie(wawsSiteCookie);
    if (cookie !== undefined) {
        $.getJSON("/api/site/" + cookie, function (data) {
            if (data != null) {
                viewModel.siteJson(data);
            } else {
                viewModel.siteJson(undefined);
                $.removeCookie(wawsSiteCookie);
            }
        });
    }
}

window.onload = function () {
    initViewModel();
    initTemplates();
    initSite();
    $("#create-site").click(function () {
        $.ajax({
            type: "POST",
            url: "/api/site",
            data: JSON.stringify(viewModel.selectedTemplate()),
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                viewModel.siteJson(data);
                $.cookie(wawsSiteCookie, data.id);
            }
        });
    });
};