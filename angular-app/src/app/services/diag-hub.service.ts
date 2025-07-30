import {inject, Injectable, OnDestroy} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import * as signalR from '@microsoft/signalr';
import { v4 as uuidv4 } from 'uuid'; // Install uuid package for UUID generation

const TAB_ID_KEY = "tabIdStorageKey"

@Injectable({
  providedIn: 'root'
})
export class DiagHubService implements OnDestroy {

  #hubConnection!: signalR.HubConnection;
  #http = inject(HttpClient);
  private readonly negotiateUrl = '/api/negotiate';
  tabId = '';
  
  constructor() {
    this.initTabId();
    
    this.#http.get<any>(this.negotiateUrl).subscribe(connectionInfo => {
      this.#hubConnection = new signalR.HubConnectionBuilder()
          .withUrl(connectionInfo.url, {accessTokenFactory: () => connectionInfo.accessToken})
          .withAutomaticReconnect()
          .build();

      this.#hubConnection.start().catch(err => console.error(err));

      this.#hubConnection.on('newMessage', (message: string) => {
        console.log(message);
        
      });
    });
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
    this.#hubConnection.stop()
      .catch(err => console.error('Error stopping hub connection:', err));
    
  }
}
