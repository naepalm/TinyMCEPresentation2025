
export async function createMentionsSelect(): Promise<(request: any, respondWith: any) => void> {
    return async (request: any, respondWith: any) => {
        try {
            
        } catch (err) {
            console.error("Error loading user detail:", err);
            respondWith(document.createTextNode("Error loading user details"));
        }
    };
}