export function createAdvTemplatesSelect() {
    return async (request: any) => {
        try {
            const response = await fetch(`/umbraco/api/advancedtemplates/gettemplate/${request}`);
            const template = await response.json();

            return {
                id: template.id.toString(),
                title: template.name,
                content: template.content
            };
        } catch (error) {
            console.error("Error fetching template:", error);
            return null; // TinyMCE handles null gracefully
        }
    };
}
