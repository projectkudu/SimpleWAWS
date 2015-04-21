interface IStep {
    id: number;
    title: string;
    sref: string;
    previous: string;
    next: string;
    onNext(): ng.IPromise<any>|void;
    onPrevious(): ng.IPromise<boolean>|void;
}

interface IAppService {
    name: string;
    sprite: string;
    steps: IStep[];
    templates: any[];
}

interface IAppControllerScope extends ng.IScope {
    currentAppService: IAppService;
    nextState(index: number): string;
    swap();
}