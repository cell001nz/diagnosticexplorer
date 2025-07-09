import { HttpClient } from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {catchError, Observable, throwError} from "rxjs";
import {getErrorMsg} from "../../util/errorUtil";

@Injectable({
  providedIn: 'root'
})
export class DataService {

  #http = inject(HttpClient)

  constructor() { }
  
  
  getData(): Observable<string> {
    return this.#http.get('api/DataTrigger', { responseType: 'text' })
        .pipe(catchError(err => throwError(() => new Error(getErrorMsg(err)))));
  }
  
}
