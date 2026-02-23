import {inject, Injectable, OnDestroy} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import * as signalR from '@microsoft/signalr';
import { v4 as uuidv4 } from 'uuid';
import {AuthService} from "@services/auth.service";
import {DiagProcess} from "@domain/DiagProcess";
import {Observable, Subject, firstValueFrom} from "rxjs";
import {OperationResponse, SetPropertyRequest} from "@domain/SetPropertyRequest";
import {DiagnosticResponse, SystemEvent} from "@domain/DiagResponse";

const TAB_ID_KEY = "tabIdStorageKey"

@Injectable({
  providedIn: 'root'
})
export class DiagHubService implements OnDestroy {

  // #hubConnection!: signalR.HubConnection;
  #http = inject(HttpClient);
  readonly #negotiateUrl = '/api/webhub/negotiate';
  #authService = inject(AuthService);
  #hubConnection?: Promise<signalR.HubConnection>;
  processArrived$ = new Subject<DiagProcess>();
  diagsArrived$ = new Subject<{processId: number, response: DiagnosticResponse}>();
  clearEvents$ = new Subject<{processId: number}>();
  streamEvents$ = new Subject<{processId: number, events: SystemEvent[] }>();
  tabId = '';
  
  constructor() {
    this.initTabId();
  }
  
  private async getHubConnection(): Promise<signalR.HubConnection> {
    if (!this.#hubConnection) {
      this.#hubConnection = new Promise(resolve => {
        this.#http.get<any>(this.#negotiateUrl).subscribe(async connectionInfo => {
          console.log('Hub connection info', connectionInfo);
          let hub = new signalR.HubConnectionBuilder()
            .withUrl(connectionInfo.url, {accessTokenFactory: () => connectionInfo.accessToken})
            .withAutomaticReconnect()
            .build();
  
          await hub.start();
          hub.on('say', (message) => console.log('Hub message', message));
          hub.on('ReceiveProcess', (process: DiagProcess) => {
            console.log('DiagHubService.ReceiveProcess', process);
            this.processArrived$.next(process);
          });
          hub.on('ReceiveDiagnostics', (processId: number, response: DiagnosticResponse) => {
            console.log('Diagnostics arrived', processId, response);
            this.diagsArrived$.next({processId, response});
          });
          hub.on('ClearEvents', (processId: number) => {
            console.log('ClearEvents', processId);
            this.clearEvents$.next({processId});
          });
          hub.on('StreamEvents', (processId: number, events: SystemEvent[]) => {
            console.log('StreamEvents', processId, events);
            this.streamEvents$.next({processId, events});
          });
          console.log('Hub connection configured');
          hub.onclose(error => console.log('Hub connection closed:', error));
          resolve(hub);
        })
      });
    }
    return this.#hubConnection; 
  }
  
  private initTabId() {
    const initTabId = (): string => {
      const id = sessionStorage.getItem(TAB_ID_KEY)
      if (id) {
        sessionStorage.removeItem(TAB_ID_KEY)
        return id;
      }
      return uuidv4()
    }

    this.tabId = initTabId()
    // window.addEventListener("beforeunload", () => sessionStorage.setItem(TAB_ID_KEY, this.#tabId));
  }

  ngOnDestroy() {
    sessionStorage.setItem(TAB_ID_KEY, this.tabId)
  }

  async subscribeSite(siteId: number) {
    let hub = await this.getHubConnection();
    const connectionId = hub.connectionId;
    console.log('Subscribe site: ' + siteId);
    await firstValueFrom(this.#http.post(`/api/webhub/SubscribeSite`, null, {
      params: { siteId: siteId, connectionId: connectionId! }
    }));
  }

  async unsubscribeSite(siteId: number) {
    let hub = await this.getHubConnection();
    const connectionId = hub.connectionId;
    await firstValueFrom(this.#http.post(`/api/webhub/UnsubscribeSite`, null, {
      params: { siteId: siteId, connectionId: connectionId! }
    }));
  }

  async subscribeProcess(processId: number) {
    let hub = await this.getHubConnection();
    const connectionId = hub.connectionId;
    console.log('Subscribe process: ' + processId + ' connectionId: ' + connectionId);
    
    await firstValueFrom(this.#http.post(`/api/webhub/SubscribeProcess`, null, {
      params: { processId: processId!, connectionId: connectionId! }
    }));
  }

  async unsubscribeProcess(processId: number) {
    let hub = await this.getHubConnection();
    const connectionId = hub.connectionId;
    console.log('Unsubscribe process: ' + processId + ' connectionId: ' + connectionId);
    
    await firstValueFrom(this.#http.post(`/api/webhub/UnsubscribeProcess`, null, {
      params: { processId: processId!, connectionId: connectionId! }
    }));
  }
    async setPropertyValue(request: SetPropertyRequest): Promise<void> {
       let hub = await this.getHubConnection();
       await hub.invoke("SetProperty", request);
       // await hub.invoke("SetProperty2", request.processId, request.siteId, request.path, request.value);
    }
}
