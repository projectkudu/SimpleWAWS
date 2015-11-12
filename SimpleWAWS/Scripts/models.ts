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
    confirmDelete?: boolean;
}

interface IAppService {
    name: string;
    sprite: string;
    title: string;
    steps: IStep[];
    templates: ITemplate[];
    hidden?: boolean;
    description: string;
}

interface ITemplate {
    name: string;
    sprite?: string;
    appService: string;
    language?: string;
    fileName?: string;
    githubRepo: string;
    description?: string;
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
    showFullBlackBlocker: boolean;
    onAppServiceMouseOver(appService: IAppService);
    onAppServiceMouseLeave();
    onTemplateMouseOver(template: ITemplate);
    onTemplateMouseLeave();
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
    downloadMobileClient(client: string, templateName: string);
    getComScorQuery();
    appTypeForQuery: string;
    cachedQuery: string;
    freeTrialTopCachedQuery: string;
    freeTrialBottomCachedQuery: string;
    freeTrialExpireCachedQuery: string;
    logout();
    experiment: string;
    branding: string;
    showFeedback: boolean;
    submittedFeedback: boolean;
    feedbackResponse: string;
    comment: string;
    contactMe: boolean;
    showShareFeedback();
    submitFeedback();
    cancelFeedback();
    currentCulture: string;
    sourceVariation: string;
}

interface IStaticDataFactory {
    getAppServices(sv?: string): IAppService[];
    getMobileClients(sampleName: string): any[];
}