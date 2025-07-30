import {inject, Injectable, resource} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Site} from "@model/Site";
import {firstValueFrom, Observable} from "rxjs";
import {rxResource} from "@angular/core/rxjs-interop";
import {DiagProcess} from "@model/DiagProcess";

@Injectable({
  providedIn: 'root'
})
export class SiteService {
  
    #http = inject(HttpClient);
    
    sites = rxResource({
        defaultValue: [],
      stream: () => this.#http.get<Site[]>('api/Sites')
    });

  getSite(id: string): Observable<Site> {
    return this.#http.get<Site>(`api/Sites/${id}`);
  }

  insertSite(site: Site): Observable<Site> {
      return this.#http.post<Site>(`api/Sites`, site);
  }
  updateSite(site: Site): Observable<Site> {
      return this.#http.put<Site>(`api/Sites/${site.id}`, site);
  }
  
  getProcesses(siteId: string) {
    return this.#http.get<DiagProcess[]>(`api/Sites/${siteId}/Processes`);
  }

  getDiagnostics(siteId: string, processId: string): Observable<string> {
    return this.#http.get(`api/Sites/${siteId}/Processes/${processId}/Diagnostics`, { responseType: "text"});
  }

  newSecret() {
       return this.#http.get(`api/Secrets/New`, { responseType: 'text'}); 
  }
}
