use super::{chat_component::ChatComponent, message_in::MessageIn, message_out::MessageOut};
use crate::inner::chat_component::Message;
use wasm_bindgen::{prelude::Closure, JsCast, JsValue};
use web_sys::{MessageEvent, WebSocket};
use yew::html::Scope;

const SERVER_URL: &str = "ws://localhost:5023/ws";

pub struct Communication {
    ws: WebSocket,
    link: Scope<ChatComponent>,
}

impl Communication {
    pub fn new(link: Scope<ChatComponent>) -> Self {
        let ws = WebSocket::new(SERVER_URL).expect("WebSocket failed");
        let selfie = Self { ws, link };
        selfie.listen();
        selfie
    }

    fn listen(&self) {
        self.ws.set_binary_type(web_sys::BinaryType::Arraybuffer);

        let link = self.link.clone();

        let onmessage_callback = Closure::wrap(Box::new(move |e: MessageEvent| {
            match e
                .data()
                .dyn_into::<JsValue>()
                .map(|value| value.as_string().unwrap_or_default())
                .map(|value| serde_json::from_str::<MessageIn>(&value))
            {
                Ok(Ok(message)) => link.send_message(Message::Receive(message)),
                _ => log::error!("Failed to parse"),
            }
        }) as Box<dyn FnMut(MessageEvent)>);

        self.ws
            .set_onmessage(Some(onmessage_callback.as_ref().unchecked_ref()));

        onmessage_callback.forget();
    }

    pub fn send(&mut self, message: &mut MessageOut) -> bool {
        match self.ws.ready_state() {
            WebSocket::OPEN => (),
            WebSocket::CONNECTING => log::warn!("Connecting"),
            _ => {
                log::warn!("Reconnect");
                self.ws = WebSocket::new(SERVER_URL).expect("WebSocket failed");
                self.listen();
            }
        }

        if self.ws.ready_state() == WebSocket::OPEN {
            match serde_json::to_string(message) {
                Ok(message) => match self.ws.send_with_str(message.as_str()) {
                    Ok(()) => return true,
                    Err(e) => log::error!("Failed to send: {:?}", e),
                },
                Err(e) => log::error!("Failed to serialize: {:?}", e),
            }
        }
        false
    }
}
