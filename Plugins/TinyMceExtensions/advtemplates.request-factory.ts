export function createAdvTemplatesRequest() {
    return async () => {
        try {
            const response = await fetch(`/umbraco/api/advancedtemplates/gettemplates`);
            const categories = await response.json();

            // Map API categories into TinyMCE expected shape
            return categories.map((cat: any) => ({
                title: cat.name, // category title
                id: cat.id,
                items: cat.items.map((t: any) => ({
                    title: t.name,
                    id: t.id,
                    content: t.content
                }))
            }));
        } catch (error) {
            console.error("Error fetching templates:", error);
            return [];
        }
    };
}
