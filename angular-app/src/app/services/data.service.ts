import { HttpClient } from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {catchError, Observable, throwError} from "rxjs";
import {Site} from "../model/Site";

@Injectable({
  providedIn: 'root'
})
export class DataService {
  #http = inject(HttpClient)

  getSites(): Observable<Site[]> {
    return this.#http.get<Site[]>('api/Sites');
  }

  getSite(id: string): Observable<Site> {
    return this.#http.get<Site>(`api/Sites/${id}`);
  }

  saveSite(site: Site): Observable<Site> {
    if (site.id)
      return this.#http.put<Site>(`api/Sites/${site.id}`, site);
    else
      return this.#http.post<Site>(`api/Sites`, site);
  }

  newSecret() {
       return this.#http.get(`api/Secrets/New`, { responseType: 'text'}); 
  }
}
