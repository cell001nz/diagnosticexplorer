import {Component, inject, resource} from '@angular/core';
import {DataService} from "../services/data.service";
import {firstValueFrom} from "rxjs";
import {JsonPipe} from "@angular/common";
import {ErrMsgPipe} from "../pipes/err-msg.pipe";

@Component({
  selector: 'app-show-data',
  imports: [
    ErrMsgPipe
  ],
  templateUrl: './show-data.component.html',
  styleUrl: './show-data.component.scss'
})
export class ShowDataComponent {

  #dataSvc = inject(DataService)
  
  myData = resource({
    defaultValue: "234",
    loader: ({}) => firstValueFrom(this.#dataSvc.getData())  
  })
  
  myData2 = resource({
    defaultValue: "2468",
    loader: ({}) => firstValueFrom(this.#dataSvc.getData2())  
  })

  protected readonly Object = Object;
  protected readonly String = String;
}
