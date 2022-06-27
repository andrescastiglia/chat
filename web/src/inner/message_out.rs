use serde::Serialize;

#[derive(Serialize)]
pub struct MessageOut {
    #[serde(rename = "Session")]
    session: String,
    #[serde(rename = "Text")]
    text: String,
}

impl MessageOut {
    pub fn new(session: &str, text: &str) -> Self {
        Self {
            session: session.to_string(),
            text: text.to_string(),
        }
    }

    pub fn session(&self) -> &str {
        self.session.as_str()
    }
    pub fn text(&self) -> &str {
        self.text.as_str()
    }
}
