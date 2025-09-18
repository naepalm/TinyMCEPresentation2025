export async function createMergeTagsRequest(): Promise<any[]> {
    try {
        const response = await fetch('/App_Plugins/tinymce-extensions-plugin/files/mergetags.json');
        return await response.json();
    } catch (error) {
        console.error("Error loading merge tags:", error);
        return [];
    }
}