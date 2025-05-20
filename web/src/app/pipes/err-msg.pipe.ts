import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'errMsg'
})
export class ErrMsgPipe implements PipeTransform {

  transform(value: unknown, ...args: unknown[]): string {
    if (!value) return '';
    
    if (typeof(value) == 'string') return value;
        
    if (typeof(value) !== 'object')
        return String(value)
    
    if ('message' in value)
      return String(value.message);
    
    return String(value);
  }

}
