interface IStep {
    id: number;
    title: string;
    sref: string;
    nextClass?: string;
    nextText?: string;
    previousClass?: string;
    previousText?: string;
    onNext?: () => ng.IPromise<any>|void;
    onPrevious?: () => ng.IPromise<boolean>|void;
}

interface IAppService {
    name: string;
    sprite: string;
    title: string;
    steps: IStep[];
    templates: ITemplate[];
}

interface ITemplate {
    name: string;
    sprite: string;
    appService: string;
    language?: string;
    fileName?: string;
}

interface IAppControllerScope extends ng.IScope {
    currentAppService: IAppService;
    nextState(index: number): string;
    currentStep: IStep;
    nextStep: IStep;
    previousStep: IStep;
    appServices: IAppService[];
    selectAppService(appService: IAppService);
    setNextAndPreviousSteps(index: number);
    getStateLink(step: IStep);
    getLanguage(template: ITemplate): string;
    selectLanguage(language: string);
    ngModels: any;
    selectedTemplate: ITemplate;
    selectTemplate(template: ITemplate);
    changeLanguage();
    goToNextState();
    goToPreviousState();
    running: boolean;
    resource: any;
    loginOptions: boolean;
    handleLoginClick(method: string);
    mobileClients: any[];
    selectedMobileClient: any;
    selectMobileClient(any);
    timeLeft: string;
    getApiSiteUrl(): string;
    siteExpired: boolean;
    dismissSiteExpired();
    confirmDelete: boolean;
    deleteResource(dontGoBack?: boolean): ng.IPromise<any>;
    freeTrialClick(place: string);
    offerDeleteAndCreate: boolean;
    initExistingState();
    deleteAndCreateResource();
    experiment: string;
}

interface ITryRootScope extends ng.IRootScopeService {
    deleteResourceClick();
    createAppType(appType: string);
    $state: any;
    $stateParams: any;
    freeTrialClick(place: string);
    ibizaClick();
    monacoClick();
    downloadContentClick();
    downloadPublishingProfileClick();
    gitLinkClick();
    downloadMobileClient(client: string);
    getComScorQuery();
    appTypeForQuery: string;
    cachedQuery: string;
    logout();
}