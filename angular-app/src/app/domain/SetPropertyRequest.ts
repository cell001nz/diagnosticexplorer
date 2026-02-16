export class SetPropertyRequest {
    processId = 0;
    path = '';
    value = '';
}

export class OperationResponse {
    isSuccess = false;
    result = '';
    errorMessage = '';
    errorDetail = '';
}
