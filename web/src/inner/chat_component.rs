use super::{communication::Communication, message_in::MessageIn, message_out::MessageOut};
use regex::Regex;
use std::ops::Not;
use uuid::Uuid;
use wasm_bindgen::JsCast;
use web_sys::{HtmlInputElement, KeyboardEvent};
use yew::{html, Component, Context, Html};

const CMD_LOGIN: &str = r"^/login\s+(\S+):(\S+)";
const CMD_LOGOFF: &str = r"^/logoff$";
const MAX_MESSAGES: usize = 50;
const RENDER: bool = true;

pub enum Message {
    Send(MessageOut),
    Receive(MessageIn),
}

pub struct ChatComponent {
    communication: Communication,
    messages: Vec<MessageIn>,
    cmd_login: Regex,
    cmd_logoff: Regex,
    session: Uuid,
}

impl Component for ChatComponent {
    type Message = Message;
    type Properties = ();

    fn create(ctx: &Context<Self>) -> Self {
        Self {
            communication: Communication::new(ctx.link().clone()),
            messages: Vec::with_capacity(MAX_MESSAGES),
            cmd_login: Regex::new(CMD_LOGIN).expect("Invalid login command regex"),
            cmd_logoff: Regex::new(CMD_LOGOFF).expect("Invalid logoff command regex"),
            session: Uuid::new_v4(),
        }
    }

    fn update(&mut self, _ctx: &Context<Self>, message: Self::Message) -> bool {
        let mut enqueue = |message: MessageIn| {
            if self.messages.len().eq(&MAX_MESSAGES) {
                self.messages.remove(0);
            }
            self.messages.push(message);
        };

        match message {
            Message::Send(mut message) => {
                if self.communication.send(&mut message).not() {
                    enqueue("Failed to send message".into());
                }
            }
            Message::Receive(message) => {
                if self.cmd_logoff.is_match(message.text()) {
                    enqueue("See you soon!".into());
                } else if let Some(groups) = self.cmd_login.captures(message.text()) {
                    let user = groups.get(1).map_or_else(|| "", |m| m.as_str());
                    enqueue(format!("Hi {}!", user).as_str().into());
                } else {
                    enqueue(message);
                }
            }
        }

        RENDER
    }

    fn view(&self, ctx: &Context<Self>) -> Html {
        let link = ctx.link();
        let session = self.session.to_string();

        let on_keypress = link.batch_callback(move |e: KeyboardEvent| {
            if e.key_code().eq(&13) {
                if let Some(Ok(input)) = e.target().map(|e| e.dyn_into::<HtmlInputElement>()) {
                    let message = MessageOut::new(session.as_str(), input.value().as_str());
                    input.set_value("");
                    return Some(Message::Send(message));
                }
            }
            None
        });

        html! {
            <div>
                <nav class="navbar bg-light">
                    <div class="container-fluid">
                        <span class="navbar-brand mb-0 h1">{ "CHAT" }</span>
                    </div>
                </nav>
                <div class="container">
                {
                self.messages.iter().map(|message| {
                    html! {
                        <div class="list-group">
                            <div class="list-group-item list-group-item-action" aria-current="true">
                                <div class="d-flex w-100 justify-content-between">
                                    <h5 class="mb-1">{ message.user() }</h5>
                                    <small>{ message.sent() }</small>
                                </div>
                                <p class="mb-1">{ message.text() }</p>
                            </div>
                        </div>
                    }
                }).collect::<Html>()
                }
                </div>
                <nav class="navbar fixed-bottom bg-light">
                    <div class="container-fluid">
                        <div class="input-group mb-3">
                            <span class="input-group-text" id="basic-addon1">{ ">" }</span>
                            <input onkeypress={ on_keypress } type="text" class="form-control" placeholder="Message" aria-label="Message" aria-describedby="basic-addon1" />
                        </div>
                    </div>
                </nav>
            </div>
        }
    }
}
