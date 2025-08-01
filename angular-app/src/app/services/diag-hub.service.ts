import {inject, Injectable, OnDestroy} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import * as signalR from '@microsoft/signalr';
import { v4 as uuidv4 } from 'uuid';
import {AuthService} from "@services/auth.service";
import {DiagProcess} from "@domain/DiagProcess";
import {Observable, Subject} from "rxjs"; // Install uuid package for UUID generation

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
  $processArrived = new Subject<DiagProcess>();
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
          hub.on('ReceiveProcesses', (siteId: string, processes: DiagProcess[]) => {
            console.log('Processes arrived', siteId, processes);
          });
          hub.on('ReceiveProcess', (process: DiagProcess) => {
            console.log('Process arrived', process);
            this.$processArrived.next(process);
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

  async subscribeSite(siteId: string) {
    let hub = await this.getHubConnection();
    console.log('Subscribe site: ' + siteId);
    await hub.invoke("SubscribeSite", siteId);
  }

  async unsubscribeSite(siteId: string) {
    let hub = await this.getHubConnection();
    await hub.invoke("UnsubscribeSite", siteId);
  }

  async subscribeProcess(processId: string) {
    let hub = await this.getHubConnection();
    await hub.invoke("SubscribeProcess", processId);
  }

  async unsubscribeProcess(processId: string) {
    let hub = await this.getHubConnection();
    await hub.invoke("UnsubscribeProcess", processId);
  }
}
