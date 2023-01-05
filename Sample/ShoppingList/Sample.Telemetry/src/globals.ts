 const _window = () => {
    // return the global native browser window object
    return window;
}

export class GlobalService{
    get nativeWindow(): any {
        return _window();
    }
}

export const metaEnv: ImportMetaEnv = import.meta.env;