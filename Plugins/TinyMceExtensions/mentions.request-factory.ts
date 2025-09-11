export async function createMentionsRequest(): Promise<(request: any, respondWith: any) => void> {

	return async (request: any, respondWith: any) => {

        try {
			// Call your custom Umbraco Members API, passing the search term
			const response = await fetch(`/umbraco/api/members/getmembers?term=${encodeURIComponent(request.term)}`);
			const members = await response.json();

			// Map the API response to TinyMCE's expected format
			const users = members.map((m: any) => ({
				id: m.id.toString(),
				name: m.name,
				description: m.description
			}));

			// Return only first 10 results
			respondWith(users);
        } catch (error) {
            console.error("Error fetching members:", error);
            respondWith([]); // fallback to empty list
        }

	};
}
