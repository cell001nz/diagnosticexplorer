import {inject, Injectable, INJECTOR, Injector, Provider, StaticProvider, Type} from "@angular/core";
import {Observable, take} from "rxjs";
import {RealtimeModel} from "@model/RealtimeModel";
import {ObservableDisposable} from "@model/ObservableDisposable";


@Injectable({providedIn: 'root'})
export class DiagnosticModelFactory {

    injector = inject(INJECTOR);    
    
    #createInjector(...providers: Array<Provider | StaticProvider>): Injector {
        return Injector.create({
            parent: this.injector,
            providers: [
                ...providers
            ]});
    }
    
    private create<T extends ObservableDisposable>(type: Type<T>, ...providers: Array<Provider | StaticProvider>)
    {
        let injector = this.#createInjector(...[type, ...providers]);
        let created = injector.get(type);
        created.disposed$.pipe(take(1))
            .subscribe(() => (injector as any).destroy());
        
        return created;
    }
    
    createRealtimeModel(): RealtimeModel {
        return this.create(RealtimeModel);
    }
    
}