import { HttpClient } from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {catchError, Observable, throwError} from "rxjs";
import {getErrorMsg} from "../../util/errorUtil";
import {Site} from "../model/site";

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
}
