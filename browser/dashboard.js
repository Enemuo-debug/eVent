const apiBase = "http://localhost:5008/events";
const authBase = "http://localhost:5008/managers";

const userDetailsDiv = document.getElementById("userDetails");
const eventsList = document.getElementById("eventsList");
const form = document.getElementById("eventForm");
const logoutBtn = document.getElementById("logoutBtn");

// 🟢 When the dashboard loads
window.addEventListener("DOMContentLoaded", async () => {
  await loadUserDetails();
  await loadEvents();
});

// 🟢 Event creation form submission
form.addEventListener("submit", async (e) => {
  e.preventDefault();

  const eventName = document.getElementById("title").value.trim();
  const eventDate = document.getElementById("date").value;
  const location = document.getElementById("location").value.trim();
  const eventDescription = document.getElementById("description").value.trim() || "...";

  const newEvent = {
    EventName: eventName,
    EventDate: eventDate,
    Location: location,
    EventDescription: eventDescription,
  };

  console.log("Creating event:", newEvent);

  try {
    const res = await fetch(`${apiBase}/create`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      credentials: "include",
      body: JSON.stringify(newEvent),
    });

    const data = await res.json();

    if (res.ok) {
      alert(data.message || "Event created successfully!");
      form.reset();
      await loadEvents();
    } else {
      alert(data.message || "Failed to create event.");
    }
  } catch (error) {
    console.error("Error creating event:", error);
    alert("Server error while creating event.");
  }
});

// 🟢 Logout functionality
logoutBtn.addEventListener("click", async () => {
  try {
    const res = await fetch(`${authBase}/logout`, {
      method: "GET",
      credentials: "include",
    });

    const data = await res.json();

    if (res.ok) {
      alert(data.message || "Logged out successfully!");
      window.location.href = "signin.html";
    } else {
      alert(data.message || "Logout failed.");
    }
  } catch (error) {
    console.error(error);
    alert("Server error during logout.");
  }
});

// 🟢 Fetch logged-in user's details
async function loadUserDetails() {
  try {
    const res = await fetch(`${authBase}/details`, {
      method: "GET",
      credentials: "include",
    });

    const result = await res.json();

    if (res.ok) {
      userDetailsDiv.innerHTML = `
        <div class="user-card">
          <h2>Welcome, ${result.userName}</h2>
          <p><strong>Email:</strong> ${result.email}</p>
          <p><strong>Organization:</strong> ${result.organizationName}</p>
          <p><strong>Plan:</strong> ${result.plan}</p>
        </div>
      `;
    } else {
      userDetailsDiv.innerHTML = `<p class="error">${result.message || "Failed to load user info."}</p>`;
      window.location = "/browser/signin.html"
    }
  } catch (error) {
    console.error(error);
    userDetailsDiv.innerHTML = `<p class="error">Server error while fetching user details.</p>`;
  }
}

async function loadEvents() {
  try {
    const res = await fetch(`${apiBase}/all`, {
      method: "GET",
      credentials: "include",
    });
    const data = await res.json();

    if (!res.ok) {
      alert(data.message || "Failed to fetch events.");
      return;
    }

    // Sort events by date ascending
    const events = Array.isArray(data.events || data) ? data.events || data : [];
    events.sort((a, b) => new Date(a.eventDate) - new Date(b.eventDate));

    displayEvents(events);
  } catch (error) {
    alert("Error fetching events.");
    console.error(error);
  }
}

// 🟢 Display events
function displayEvents(events) {
  eventsList.innerHTML = "";
  if (!events || events.length === 0) {
    eventsList.innerHTML = "<p>No events found.</p>";
    return;
  }

  events.forEach((event) => {
    const eventCard = document.createElement("div");
    eventCard.className = "event-card";
    
    if (!event.isLive)
    {
      eventCard.innerHTML = `
        <div class="event-content">
          <h3>${event.eventName}</h3>
          <p><strong>Date:</strong> ${new Date(event.eventDate).toLocaleDateString()}</p>
          <p><strong>Location:</strong> ${event.location}</p>
          <p>${event.eventDescription}</p>
        </div>
        <div class="actions">
          <button class="delete-btn">Delete</button>
          <button class="live-btn">Publish</button>
        </div>
      `;
    } else {eventCard.innerHTML = `
        <div class="event-content">
          <h3>${event.eventName}</h3>
          <p><strong>Date:</strong> ${new Date(event.eventDate).toLocaleDateString()}</p>
          <p><strong>Location:</strong> ${event.location}</p>
          <p>${event.eventDescription}</p>
        </div>
        <div class="actions">
          <button class="delete-btn">Delete</button>
          <em><h3 id="live">The event is live...</h3></em>
        </div>
      `;
    }

    // 🟢 Click event for editing
    eventCard.querySelector(".event-content").addEventListener("click", () => {
      window.location.href = `edit.html?id=${event.id}`;
    });

    // 🟢 Delete event
    const deleteBtn = eventCard.querySelector(".delete-btn");
    deleteBtn.addEventListener("click", async (e) => {
      e.stopPropagation();
      const confirmDelete = confirm(`Are you sure you want to delete "${event.eventName}"?`);
      if (!confirmDelete) return;

      try {
        const res = await fetch(`${apiBase}/${event.id}`, {
          method: "DELETE",
          credentials: "include",
        });
        const data = await res.json();

        if (res.ok) {
          alert(data.message || "Event deleted successfully!");
          await loadEvents();
        } else {
          alert(data.message || "Failed to delete event.");
        }
      } catch (error) {
        console.error("Error deleting event:", error);
        alert("Server error while deleting event.");
      }
    });

    // 🟢 Publish event
    const liveBtn = eventCard.querySelector(".live-btn");
    if (liveBtn)
    {
      liveBtn.addEventListener("click", async (e) => {
        e.stopPropagation();
        const confirmPublish = confirm(
          "Make sure the event is fully formed — you can no longer edit after publishing."
        );
        if (!confirmPublish) return;

        try {
          const res = await fetch(`${apiBase}/publish/${event.id}`, {
            method: "PUT",
            credentials: "include",
          });
          const data = await res.json();

          if (res.ok) {
            alert(data.message || "Event published successfully!");
            await loadEvents();
          } else {
            alert(data.message || "Failed to publish event.");
          }
        } catch (error) {
          console.error("Error publishing event:", error);
          alert("Error publishing event.");
        }
      });
    }

    eventsList.appendChild(eventCard);
  });
} 