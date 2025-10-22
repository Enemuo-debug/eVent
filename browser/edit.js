const apiBase = "http://localhost:5008/events";
const authBase = "http://localhost:5008/managers";

const form = document.getElementById("editForm");
const backBtn = document.getElementById("backBtn");

const urlParams = new URLSearchParams(window.location.search);
const eventId = urlParams.get("id");

// Go back to dashboard
backBtn.addEventListener("click", () => {
  window.location.href = "dashboard.html";
});

// Load event details
window.addEventListener("DOMContentLoaded", async () => {
  if (!eventId) {
    alert("No event ID provided.");
    window.location.href = "dashboard.html";
    return;
  }
  await loadEventDetails(eventId);
});

// 🟢 Fetch event by ID and prefill form + mapper
async function loadEventDetails(id) {
  try {
    const res = await fetch(`${apiBase}/${id}`, {
      method: "GET",
      credentials: "include",
    });
    const data = await res.json();

    if (res.ok) {
      if (data.isLive) {
        document.getElementById("updateForm").style.display = "none";
        document.getElementById("map").style.display = "none";
        document.getElementById("eventDesc").style.display = "block";
        document.getElementById("emailSection").style.display = "block";

        document.getElementById("eventName").innerHTML = data.eventName;
        document.getElementById("eventDate").innerHTML = data.eventDate
          .split("T")[0]
          .split("-")
          .reverse()
          .join("-");
        document.getElementById("eventLocation").innerHTML = data.location;
        document.getElementById("eventDescription").innerHTML = data.eventDescription;

        document.getElementById("copy-form-link").addEventListener("click", async () => {
          await navigator.clipboard.writeText(
            `http://127.0.0.1:5500/browser/form.html?id=${data.id}`
          );
          alert("Form link copied to clipboard");
        });

        renderRegisteredUsers(eventId);
      }

      // Fill the main event fields
      document.getElementById("title").value = data.eventName;
      document.getElementById("date").value = data.eventDate.split("T")[0];
      document.getElementById("location").value = data.location || "";
      document.getElementById("description").value = data.eventDescription || "";

      mapperList.innerHTML = "";

      // If using the string format for form description
      if (data.formDescription && typeof data.formDescription === "string") {
        const parts = data.formDescription.split("@").filter((x) => x.trim() !== "");
        parts.forEach((field) => {
          const [name, desc, type] = field.split("->");
          if (name && desc && type) addFieldRow(name.trim(), type.trim(), desc.trim());
        });
      }
      // Or array format
      else if (Array.isArray(data.formMapper)) {
        data.formMapper.forEach((field) => {
          addFieldRow(field.name, field.type, field.description || "");
        });
      }
    } else {
      alert(data.message || "Failed to load event details.");
      window.location.href = "dashboard.html";
    }
  } catch (error) {
    console.error("Error loading event:", error);
    alert("Server error loading event details.");
  }
}

form.addEventListener("submit", async (e) => {
  e.preventDefault();

  // Validate before submission
  if (!validateForm()) return;

  const updatedEvent = {
    EventName: document.getElementById("title").value.trim(),
    EventDate: document.getElementById("date").value,
    Location: document.getElementById("location").value.trim(),
    EventDescription: document.getElementById("description").value.trim(),
    FormDescription: getMappedFields(),
  };

  try {
    const res = await fetch(`${apiBase}/edit/${eventId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      credentials: "include",
      body: JSON.stringify(updatedEvent),
    });

    let data;
    try {
      data = await res.json();
    } catch {
      data = { message: await res.text() };
    }

    if (res.ok) {
      alert(data.message || "Event updated successfully!");
      window.location.href = "dashboard.html";
    } else {
      alert(data.message || `Error ${res.status}: ${res.statusText}`);
    }
  } catch (error) {
    console.error("Error updating event:", error);
    alert("Server error while updating event: " + error.message);
  }
});

const mapperList = document.getElementById("mapperList");
const addFieldBtn = document.getElementById("addFieldBtn");

addFieldBtn?.addEventListener("click", () => addFieldRow("", "text", ""));

function addFieldRow(name = "", type = "text", description = "") {
  const row = document.createElement("div");
  row.classList.add("mapper-row");

  const nameInput = document.createElement("input");
  nameInput.type = "text";
  nameInput.placeholder = "Field Name (e.g. Department)";
  nameInput.value = name;
  nameInput.required = true;

  const typeSelect = document.createElement("select");
  ["text", "date", "email", "phone", "number"].forEach((opt) => {
    const option = document.createElement("option");
    option.value = opt;
    option.textContent = opt.charAt(0).toUpperCase() + opt.slice(1);
    if (opt === type) option.selected = true;
    typeSelect.appendChild(option);
  });

  const descInput = document.createElement("input");
  descInput.type = "text";
  descInput.placeholder = "Description (e.g. Enter department name)";
  descInput.value = description;
  descInput.required = true;

  const deleteBtn = document.createElement("button");
  deleteBtn.textContent = "×";
  deleteBtn.classList.add("delete-mapper-btn");
  deleteBtn.addEventListener("click", () => mapperList.removeChild(row));

  row.append(nameInput, typeSelect, descInput, deleteBtn);
  mapperList.appendChild(row);
}

function getMappedFields() {
  let fields = "";
  document.querySelectorAll(".mapper-row").forEach((row) => {
    const inputs = row.querySelectorAll("input[type='text']");
    const name = inputs[0].value.trim();
    const type = row.querySelector("select").value;
    const desc = inputs[1]?.value.trim();

    if (name && desc) fields += `${name}->${desc}->${type}@`;
  });
  return fields.endsWith("@") ? fields.slice(0, -1) : fields;
}

function validateForm() {
  const title = document.getElementById("title").value.trim();
  const date = document.getElementById("date").value.trim();
  const location = document.getElementById("location").value.trim();
  const description = document.getElementById("description").value.trim();

  if (!title || !date || !location || !description) {
    alert("Please fill in all the main event fields.");
    return false;
  }

  const rows = document.querySelectorAll(".mapper-row");
  for (const row of rows) {
    const inputs = row.querySelectorAll("input[type='text']");
    const name = inputs[0].value.trim();
    const desc = inputs[1]?.value.trim();

    if (!name || !desc) {
      alert("Please fill in all form mapper fields before submitting.");
      return false;
    }
  }

  return true;
}

async function renderRegisteredUsers(eventId) {
  try {
    const res = await fetch(`http://localhost:5008/events/form/${eventId}`, {
      method: "GET",
      credentials: "include",
    });
    const data = await res.json();

    if (!res.ok) {
      alert(data.message || "Failed to load registered users.");
      return;
    }

    if (!Array.isArray(data) || data.length <= 1) {
      document.getElementById("registeredUsers").innerHTML = `
        <p class="empty">No registered users found for this event.</p>
      `;
      return;
    }

    // ✅ Corrected Build Flexbox Rows
    const usersHtml = data
      .map((row) => {
        const checkedIn = row[row.length - 1];
        const trimmedRow = row.slice(0, -1);
        return `
          <div class="user-row">
            ${trimmedRow.map((value) => `<div class="user-cell">${value}</div>`).join("")}
            <div class="user-cell check-status">${checkedIn}</div>
          </div>
        `;
      })
      .join("");

    document.getElementById("registeredUsers").innerHTML = `
      <div class="registered-list">${usersHtml}</div>
    `;
  } catch (error) {
    console.error("Error loading registered users:", error);
    alert("Server error loading registered users.");
  }
}

const emailForm = document.getElementById("emailForm");

emailForm?.addEventListener("submit", async (e) => {
  e.preventDefault();

  const subject = document.getElementById("emailSubject").value.trim();
  const body = document.getElementById("emailBody").value.trim();

  if (!subject || !body) {
    alert("Please enter both subject and message.");
    return;
  }

  try {
    const res = await fetch(`http://localhost:5008/events/send-mails/${eventId}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      credentials: "include",
      body: JSON.stringify({ subject, body }),
    });

    const data = await res.json();
    console.log(data);
    if (res.ok) {
      alert(data.message || "Emails sent successfully!");
      emailForm.reset();
    } else {
      alert(data.message || "Failed to send emails.");
    }
  } catch (error) {
    console.error("Error sending emails:", error);
    alert("Server error sending emails.");
  }
});
