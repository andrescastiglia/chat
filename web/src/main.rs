use web::inner::chat_component::ChatComponent;

fn main() {
    wasm_logger::init(wasm_logger::Config::default());
    yew::start_app::<ChatComponent>();
}
