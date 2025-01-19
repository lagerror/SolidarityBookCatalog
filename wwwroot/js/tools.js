function getURLParams() {
    const searchParams = window.location.search.substring(1).split('&');
    const params = {
    };
    for (const param of searchParams) {
        const [key, value] = param.split('=');
        params[key] = value;
    }
    return params;
}