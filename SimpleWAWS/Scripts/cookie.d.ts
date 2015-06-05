 declare module Cookies {
     export function get(key: string): string;
     export function set(key: string, value: string, config?: any);
}