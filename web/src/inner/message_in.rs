use js_sys::Date;
use serde::Deserialize;

#[derive(Debug, Deserialize)]
pub struct MessageIn {
    #[serde(rename = "User")]
    user: String,
    #[serde(rename = "Text")]
    text: String,
    #[serde(rename = "Sent")]
    sent: String,
}

impl From<&str> for MessageIn {
    fn from(error: &str) -> Self {
        let now = Date::new_0();
        Self {
            user: "(qsecofr)".to_string(),
            text: error.to_string(),
            sent: format!("{:2}:{:02}", now.get_hours(), now.get_minutes()),
        }
    }
}

impl MessageIn {
    pub fn user(&self) -> &str {
        self.user.as_str()
    }
    pub fn text(&self) -> &str {
        self.text.as_str()
    }
    pub fn sent(&self) -> &str {
        self.sent.as_str()
    }
}
