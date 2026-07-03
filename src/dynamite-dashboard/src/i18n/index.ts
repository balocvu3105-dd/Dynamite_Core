import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type Language = 'vi' | 'en'

export interface Translation {
  common: {
    logout: string
    loading: string
    save: string
    cancel: string
    refresh: string
    search: string
    status: string
    actions: string
    success: string
    error: string
    enabled: string
    disabled: string
    channel: string
    role: string
    selectChannel: string
    selectRole: string
    saveChanges: string
    savedSuccess: string
  }
  sidebar: {
    overview: string
    setupEngine: string
    economy: string
    moderation: string
    settings: string
    logs: string
    welcome: string
    security: string
    botDashboard: string
    allServers: string
  }
  overview: {
    title: string
    subtitle: string
    channels: string
    roles: string
    modules: string
    serverStatus: string
    activeModules: string
    quickActions: string
    openSetup: string
    openEconomy: string
    openSecurity: string
  }
  setup: {
    title: string
    subtitle: string
    instantPresets: string
    customEngine: string
    customDesc: string
    topicLabel: string
    scaleLabel: string
    topics: {
      general: string
      gaming: string
      study: string
      tech: string
    }
    scales: {
      small: string
      medium: string
      large: string
    }
    toggles: {
      economy: string
      tickets: string
      staffMod: string
      voiceRooms: string
    }
    generateBtn: string
    generating: string
    blueprintTitle: string
    readyDeploy: string
    rolesToCreate: string
    channelLayout: string
    deployHint: string
  }
  economy: {
    title: string
    subtitle: string
    inspectWallet: string
    searchUser: string
    searchBtn: string
    grantCoins: string
    deductCoins: string
    amountPlaceholder: string
    leaderboard: string
    rank: string
    user: string
    balance: string
    actions: string
    noUsers: string
    walletDetails: string
    currentBalance: string
  }
  moderation: {
    title: string
    subtitle: string
    warnUser: string
    userIdPlaceholder: string
    reasonPlaceholder: string
    warnBtn: string
    recentWarnings: string
    auditLogs: string
    moderator: string
    target: string
    reason: string
    date: string
    noWarnings: string
    noLogs: string
  }
  logging: {
    title: string
    subtitle: string
    msgLog: string
    msgLogDesc: string
    memberLog: string
    memberLogDesc: string
    voiceLog: string
    voiceLogDesc: string
    serverLog: string
    serverLogDesc: string
  }
  welcome: {
    title: string
    subtitle: string
    enableWelcome: string
    welcomeChannel: string
    welcomeMsg: string
    welcomeMsgPlaceholder: string
    verifyChannel: string
    verifyRole: string
  }
  security: {
    title: string
    subtitle: string
    enableSecurity: string
    antiSpam: string
    antiSpamDesc: string
    msgThreshold: string
    msgWindow: string
    antiInvite: string
    antiInviteDesc: string
    antiScam: string
    antiScamDesc: string
    antiRaid: string
    antiRaidDesc: string
    raidThreshold: string
  }
}

const dictionaries: Record<Language, Translation> = {
  vi: {
    common: {
      logout: 'Đăng xuất',
      loading: 'Đang tải...',
      save: 'Lưu thay đổi',
      cancel: 'Hủy',
      refresh: 'Làm mới',
      search: 'Tìm kiếm...',
      status: 'Trạng thái',
      actions: 'Thao tác',
      success: 'Thành công',
      error: 'Có lỗi xảy ra',
      enabled: 'Bật',
      disabled: 'Tắt',
      channel: 'Kênh',
      role: 'Vai trò',
      selectChannel: '-- Chọn kênh --',
      selectRole: '-- Chọn vai trò --',
      saveChanges: 'Lưu Cấu Hình',
      savedSuccess: 'Đã lưu cấu hình thành công!',
    },
    sidebar: {
      overview: 'Tổng quan',
      setupEngine: 'Chuyên gia Thiết lập',
      economy: 'Kinh tế & Câu cá',
      moderation: 'Quản trị viên',
      settings: 'Cấu hình máy chủ',
      logs: 'Nhật ký Hoạt động',
      welcome: 'Lời chào mừng',
      security: 'Bảo mật Máy chủ',
      botDashboard: 'Bảng Điều Khiển Bot',
      allServers: 'Tất cả máy chủ',
    },
    overview: {
      title: 'Tổng Quan Máy Chủ',
      subtitle: 'Trạng thái hoạt động thời gian thực và thông số cấu trúc của máy chủ.',
      channels: 'Kênh Văn Bản & Thoại',
      roles: 'Vai Trò (Roles)',
      modules: 'Tính Năng Hoạt Động',
      serverStatus: 'Trạng Thái Máy Chủ',
      activeModules: 'Các Mô-đun Đang Bật',
      quickActions: 'Điều Hướng Nhanh',
      openSetup: 'Mở Chuyên gia Thiết lập',
      openEconomy: 'Quản lý Kinh tế & Ví',
      openSecurity: 'Cấu hình Bảo mật Anti-Spam',
    },
    setup: {
      title: 'Hệ thống Thiết lập Thông minh (Smart Setup Engine)',
      subtitle: 'Tự động tạo kênh, phân quyền và luật bảo vệ máy chủ chuẩn chuyên gia chỉ với 1 cú nhấp chuột.',
      instantPresets: 'Các Mẫu Chuyên Gia Có Sẵn',
      customEngine: 'Bộ Kiến Tạo Thiết Lập Tùy Chỉnh',
      customDesc: 'Tùy chỉnh vai trò và danh mục kênh theo định hướng và quy mô cộng đồng của bạn.',
      topicLabel: 'Chủ đề / Định hướng máy chủ',
      scaleLabel: 'Quy mô Thành viên Dự kiến',
      topics: {
        general: 'Cộng đồng Tổng hợp (General)',
        gaming: 'Hội quán Game & Esports',
        study: 'CLB Học tập & Nghiên cứu',
        tech: 'Cộng đồng Công nghệ & Crypto',
      },
      scales: {
        small: 'Nhỏ (< 100 thành viên)',
        medium: 'Trung bình (100 - 1,000 thành viên)',
        large: 'Lớn (> 1,000 thành viên)',
      },
      toggles: {
        economy: 'Kinh tế & Casino',
        tickets: 'Hỗ trợ (Ticket)',
        staffMod: 'Quản trị viên',
        voiceRooms: 'Phòng thoại tự động',
      },
      generateBtn: 'Khởi tạo Sơ đồ Kiến trúc',
      generating: 'Đang tính toán cấu trúc...',
      blueprintTitle: 'Sơ đồ Đề xuất:',
      readyDeploy: 'Sẵn sàng Áp dụng',
      rolesToCreate: 'Danh sách Vai trò mới',
      channelLayout: 'Cấu trúc Kênh đề xuất',
      deployHint: '💡 Để áp dụng trực tiếp cấu hình này lên Discord, hãy gõ lệnh `/setup smart` trong máy chủ của bạn.',
    },
    economy: {
      title: 'Quản lý Kinh Tế & Bảng Xếp Hạng',
      subtitle: 'Theo dõi tài sản thành viên, kiểm tra số dư và quản lý tiền tệ trong máy chủ.',
      inspectWallet: 'Kiểm tra & Chỉnh sửa ví',
      searchUser: 'ID người dùng Discord...',
      searchBtn: 'Tra cứu',
      grantCoins: 'Cộng tiền',
      deductCoins: 'Trừ tiền',
      amountPlaceholder: 'Số lượng Vàng...',
      leaderboard: 'Bảng Xếp Hạng Đại Gia',
      rank: 'Hạng',
      user: 'Thành viên',
      balance: 'Số dư (Vàng)',
      actions: 'Quản lý',
      noUsers: 'Chưa có dữ liệu người dùng nào trong bảng xếp hạng.',
      walletDetails: 'Chi tiết Ví Tiền',
      currentBalance: 'Số dư hiện tại:',
    },
    moderation: {
      title: 'Quản Trị Viên & Cảnh Báo',
      subtitle: 'Xử lý vi phạm, gửi lời cảnh cáo và kiểm tra lịch sử hành vi của thành viên.',
      warnUser: 'Gửi Cảnh Báo cho Thành Viên',
      userIdPlaceholder: 'ID Người Dùng Discord...',
      reasonPlaceholder: 'Lý do vi phạm (ví dụ: Spam tin nhắn)...',
      warnBtn: 'Gửi Cảnh Báo',
      recentWarnings: 'Cảnh Báo Gần Đây',
      auditLogs: 'Lịch Sử Kiểm Duyệt (Audit Logs)',
      moderator: 'Người xử lý',
      target: 'Đối tượng',
      reason: 'Lý do',
      date: 'Thời gian',
      noWarnings: 'Chưa có cảnh báo vi phạm nào.',
      noLogs: 'Chưa có nhật ký ghi nhận vi phạm.',
    },
    logging: {
      title: 'Cấu Hình Nhật Ký Hoạt Động',
      subtitle: 'Lựa chọn các kênh để tự động thông báo các sự kiện trong máy chủ.',
      msgLog: 'Nhật Ký Tin Nhắn (Message Logs)',
      msgLogDesc: 'Ghi lại khi có tin nhắn bị xóa hoặc chỉnh sửa.',
      memberLog: 'Nhật Ký Thành Viên (Member Logs)',
      memberLogDesc: 'Ghi lại khi có thành viên vào, ra hoặc đổi biệt danh.',
      voiceLog: 'Nhật Ký Phòng Thoại (Voice Logs)',
      voiceLogDesc: 'Ghi lại khi thành viên tham gia, rời hoặc bật webcam trong kênh thoại.',
      serverLog: 'Nhật Ký Máy Chủ (Server Logs)',
      serverLogDesc: 'Ghi lại khi có thay đổi vai trò hoặc cập nhật kênh.',
    },
    welcome: {
      title: 'Lời Chào Mừng & Xác Thực',
      subtitle: 'Tự động gửi thiệp chào mừng thành viên mới và cài đặt vai trò xác thực bảo vệ máy chủ.',
      enableWelcome: 'Bật hệ thống chào mừng',
      welcomeChannel: 'Kênh gửi thông báo chào mừng',
      welcomeMsg: 'Nội dung lời chào mừng',
      welcomeMsgPlaceholder: 'Chào mừng {user} đã đến với {server}!',
      verifyChannel: 'Kênh xác thực (Verify)',
      verifyRole: 'Vai trò cấp sau khi xác thực',
    },
    security: {
      title: 'Bảo Mật Máy Chủ & Chống Spam',
      subtitle: 'Tự động phát hiện spam, quảng cáo trái phép và tấn công càn quét (Raid).',
      enableSecurity: 'Bật lá chắn bảo mật tự động',
      antiSpam: 'Chống Spam Tin Nhắn',
      antiSpamDesc: 'Tự động ngăn chặn gửi tin nhắn liên tục với tốc độ bất thường.',
      msgThreshold: 'Giới hạn tin nhắn tối đa',
      msgWindow: 'Trong khoảng thời gian (giây)',
      antiInvite: 'Chặn Link Mời Máy Chủ Khác (Anti-Invite)',
      antiInviteDesc: 'Tự động xóa các link quảng cáo mời sang máy chủ Discord khác.',
      antiScam: 'Chặn Link Lừa Đảo Phishing (Anti-Scam)',
      antiScamDesc: 'Phát hiện và tiêu diệt đường link độc hại trộm tài khoản.',
      antiRaid: 'Phòng Thủ Càn Quét (Anti-Raid)',
      antiRaidDesc: 'Bật chế độ cách ly khẩn cấp nếu có lượng lớn tài khoản ảo tham gia cùng lúc.',
      raidThreshold: 'Ngưỡng cảnh báo Raid (thành viên mới/phút)',
    },
  },
  en: {
    common: {
      logout: 'Logout',
      loading: 'Loading...',
      save: 'Save changes',
      cancel: 'Cancel',
      refresh: 'Refresh',
      search: 'Search...',
      status: 'Status',
      actions: 'Actions',
      success: 'Success',
      error: 'Error occurred',
      enabled: 'Enabled',
      disabled: 'Disabled',
      channel: 'Channel',
      role: 'Role',
      selectChannel: '-- Select Channel --',
      selectRole: '-- Select Role --',
      saveChanges: 'Save Configuration',
      savedSuccess: 'Configuration saved successfully!',
    },
    sidebar: {
      overview: 'Overview',
      setupEngine: 'Server Setup',
      economy: 'Economy & Fishing',
      moderation: 'Moderation',
      settings: 'Server Settings',
      logs: 'Logging',
      welcome: 'Welcome',
      security: 'Security',
      botDashboard: 'Bot Dashboard',
      allServers: 'All servers',
    },
    overview: {
      title: 'Server Overview',
      subtitle: 'Real-time operational metrics and architectural stats for your server.',
      channels: 'Text & Voice Channels',
      roles: 'Total Roles',
      modules: 'Active Modules',
      serverStatus: 'Server Health Status',
      activeModules: 'Enabled Subsystems',
      quickActions: 'Quick Navigation',
      openSetup: 'Launch Setup Architect',
      openEconomy: 'Manage Economy & Wallets',
      openSecurity: 'Configure Anti-Spam Security',
    },
    setup: {
      title: 'Smart Server Setup Architect',
      subtitle: 'Generate an optimized server structure with instant setup and 100% rollback protection.',
      instantPresets: 'Instant Preset Templates',
      customEngine: 'Custom Smart Setup Engine',
      customDesc: 'Tailor roles and categories to your community size and activity requirements.',
      topicLabel: 'Community Theme / Topic',
      scaleLabel: 'Expected Scale',
      topics: {
        general: 'General Community',
        gaming: 'Esports & Gaming Guild',
        study: 'Study & Research Club',
        tech: 'Tech & Crypto Hub',
      },
      scales: {
        small: 'Small (< 100 members)',
        medium: 'Medium (100 - 1,000 members)',
        large: 'Large (> 1,000 members)',
      },
      toggles: {
        economy: 'Economy & Casino',
        tickets: 'Support Tickets',
        staffMod: 'Staff Moderation',
        voiceRooms: 'Auto Voice Rooms',
      },
      generateBtn: 'Generate Architecture Plan',
      generating: 'Calculating Blueprint...',
      blueprintTitle: 'Generated Blueprint:',
      readyDeploy: 'Ready to Deploy',
      rolesToCreate: 'Roles to Create',
      channelLayout: 'Proposed Channel Layout',
      deployHint: '💡 To apply this setup to Discord, run `/setup smart` inside your server.',
    },
    economy: {
      title: 'Economy & Fishing Manager',
      subtitle: 'Monitor server wealth distribution, inspect user balances, and manage wallet currencies.',
      inspectWallet: 'Inspect User Wallet',
      searchUser: 'Discord User ID...',
      searchBtn: 'Inspect',
      grantCoins: 'Grant Coins',
      deductCoins: 'Deduct Coins',
      amountPlaceholder: 'Gold amount...',
      leaderboard: 'Wealth Leaderboard',
      rank: 'Rank',
      user: 'Member',
      balance: 'Balance (Gold)',
      actions: 'Management',
      noUsers: 'No users found on the wealth leaderboard.',
      walletDetails: 'Wallet Details',
      currentBalance: 'Current Balance:',
    },
    moderation: {
      title: 'Moderation & Warning Center',
      subtitle: 'Issue warnings, review infractions, and audit moderator actions.',
      warnUser: 'Issue Member Warning',
      userIdPlaceholder: 'Discord User ID...',
      reasonPlaceholder: 'Reason for infraction (e.g. Excessive spam)...',
      warnBtn: 'Issue Warning',
      recentWarnings: 'Recent Infractions',
      auditLogs: 'Moderation Audit Logs',
      moderator: 'Moderator',
      target: 'Target',
      reason: 'Reason',
      date: 'Timestamp',
      noWarnings: 'No warnings recorded.',
      noLogs: 'No moderation logs recorded.',
    },
    logging: {
      title: 'System Logging Channels',
      subtitle: 'Select channels where the bot will report server events automatically.',
      msgLog: 'Message Logs',
      msgLogDesc: 'Records deleted and edited messages.',
      memberLog: 'Member Logs',
      memberLogDesc: 'Records user joins, leaves, and nickname changes.',
      voiceLog: 'Voice Logs',
      voiceLogDesc: 'Records voice channel activity and streaming status.',
      serverLog: 'Server Logs',
      serverLogDesc: 'Records role updates and channel modifications.',
    },
    welcome: {
      title: 'Welcome & Verification Hub',
      subtitle: 'Configure automated greeting cards and role verification protection.',
      enableWelcome: 'Enable Welcome System',
      welcomeChannel: 'Welcome Notification Channel',
      welcomeMsg: 'Welcome Message Template',
      welcomeMsgPlaceholder: 'Welcome {user} to {server}!',
      verifyChannel: 'Verification Gate Channel',
      verifyRole: 'Verified Member Role',
    },
    security: {
      title: 'Security & Anti-Spam Shield',
      subtitle: 'Automated protection against spam, malicious links, and raid attacks.',
      enableSecurity: 'Enable Automated Security Shield',
      antiSpam: 'Anti-Spam Filter',
      antiSpamDesc: 'Automatically throttle and restrict repetitive messaging.',
      msgThreshold: 'Max Messages Threshold',
      msgWindow: 'Time Window (Seconds)',
      antiInvite: 'Anti-Invite Protection',
      antiInviteDesc: 'Automatically delete unauthorized Discord server invites.',
      antiScam: 'Anti-Scam Phishing Shield',
      antiScamDesc: 'Detect and eliminate malicious account-stealing links.',
      antiRaid: 'Anti-Raid Lockdown',
      antiRaidDesc: 'Trigger emergency isolation when multiple accounts join rapidly.',
      raidThreshold: 'Raid Threshold (Joins per Minute)',
    },
  },
}

interface LangState {
  lang: Language
  t: Translation
  setLanguage: (lang: Language) => void
  toggleLanguage: () => void
}

export const useLangStore = create<LangState>()(
  persist(
    (set, get) => ({
      lang: 'vi',
      t: dictionaries.vi,
      setLanguage: (lang: Language) => set({ lang, t: dictionaries[lang] }),
      toggleLanguage: () => {
        const next = get().lang === 'vi' ? 'en' : 'vi'
        set({ lang: next, t: dictionaries[next] })
      },
    }),
    {
      name: 'dynamite-lang',
      partialize: (state) => ({ lang: state.lang }),
      onRehydrateStorage: () => (state) => {
        if (state && state.lang) {
          state.t = dictionaries[state.lang]
        }
      },
    }
  )
)
