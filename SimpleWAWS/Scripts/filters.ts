angular.module("tryApp")
    .filter("filterBySelectedLanguage",() => {
        return (templates: ITemplate[], language: string): any => {
            if (language === undefined)
                return templates;
            else
                return templates.filter(t => t.language.toUpperCase() === language.toUpperCase());
        };
    });
