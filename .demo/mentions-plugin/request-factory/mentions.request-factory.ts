export async function createMentionsRequest(): Promise<(request: any, respondWith: any) => void> {

	return (request: any, respondWith: any) => {

        // The initial hardcoded list of users where the user object must contain the properties `id` and `name`
        // More information can be found at https://www.tiny.cloud/docs/tinymce/6/mentions
        const users = [
            {
                id: "1",
                name: 'John Smith'
            },
            {
                id: "2",
                name: 'Joe Cool'
            },
            {
                id: "3",
                name: 'Zander Geulph'
            }

        ];

        // request.term is the text the user typed after the '@' - the TinyMCE documentation it's 'query.term'
        var filteredUsers = users.filter(user =>
            user.name.toLowerCase().includes(request.term.toLowerCase())
        );

        // Only get the first ten users
        filteredUsers = filteredUsers.slice(0, 10);

        // Send the users back to the editor
        respondWith(filteredUsers);
	};
}