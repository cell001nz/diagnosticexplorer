export class SetPropertyRequest {
    processId = '';
    siteId = '';
    path = '';
    value = '';
}

export class OperationResponse {
    isSuccess = false;
    result = '';
    errorMessage = '';
    errorDetail = '';
}
